using Azure.Data.Tables;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using SEAPIRATE.Models;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace SEAPIRATE.Services;

public class AuthenticationService
{
    private readonly TableClient _tableClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
    {
        var connectionString = configuration.GetConnectionString("AzureStorage")
                              ?? configuration["AzureStorage:ConnectionString"];

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Azure Storage connection string is not configured. Please check your appsettings.json file.");
        }

        var serviceClient = new TableServiceClient(connectionString);
        _tableClient = serviceClient.GetTableClient("Users");
        _tableClient.CreateIfNotExists();
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<(bool Success, string Message)> RegisterUserAsync(string username, string email, string password, string displayName = "")
    {
        try
        {
            // Normalize inputs
            var normalizedUsername = username.ToLowerInvariant().Trim();
            var normalizedEmail = email.ToLowerInvariant().Trim();

            // Check if user already exists
            var existingUser = await GetUserByUsernameAsync(normalizedUsername);
            if (existingUser != null)
                return (false, "Username already exists.");

            // Check if email already exists
            var existingEmail = await GetUserByEmailAsync(normalizedEmail);
            if (existingEmail != null)
                return (false, "Email already exists.");

            // Create new user
            var salt = GenerateSalt();
            var passwordHash = HashPassword(password, salt);

            var user = new UserEntity
            {
                RowKey = normalizedUsername,
                Username = username, // Keep original case for display
                Email = normalizedEmail,
                PasswordHash = passwordHash,
                Salt = salt,
                DisplayName = string.IsNullOrEmpty(displayName) ? username : displayName,
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow
            };

            await _tableClient.AddEntityAsync(user);
            return (true, "Registration successful!");
        }
        catch (Exception ex)
        {
            return (false, $"Registration failed: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> LoginAsync(string username, string password)
    {
        try
        {
            var normalizedUsername = username.ToLowerInvariant().Trim();
            var user = await GetUserByUsernameAsync(normalizedUsername);
            
            if (user == null)
                return (false, "Invalid username or password.");

            if (!user.IsActive)
                return (false, "Account is disabled.");

            var passwordHash = HashPassword(password, user.Salt);
            if (passwordHash != user.PasswordHash)
                return (false, "Invalid username or password.");

            // Update last login
            user.LastLoginAt = DateTime.UtcNow;
            await _tableClient.UpdateEntityAsync(user, user.ETag);

            // Perform login
            await PerformLoginAsync(user);

            return (true, "Login successful!");
        }
        catch (Exception ex)
        {
            return (false, $"Login failed: {ex.Message}");
        }
    }

    // In Azure Table Storage, a good PartitionKey is one that enables efficient queries and scales well.
    // For user authentication, a common pattern is to use a constant PartitionKey (e.g., "User") for all users,
    // and use the username (or user ID) as the RowKey. This allows you to query by PartitionKey for all users,
    // and by PartitionKey + RowKey for a specific user (which is very efficient).

    private async Task PerformLoginAsync(UserEntity user)
    {
        // Create authentication cookie
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim("DisplayName", user.DisplayName),
            new Claim("UserId", user.RowKey)
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
        };

        if (_httpContextAccessor.HttpContext != null)
        {
            if (!_httpContextAccessor.HttpContext.Response.HasStarted)
            {
                await _httpContextAccessor.HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                // Notify authentication state provider
                if (_httpContextAccessor.HttpContext.RequestServices.GetService<AuthenticationStateProvider>()
                    is CustomAuthenticationStateProvider authStateProvider)
                {
                    authStateProvider.NotifyUserAuthentication(new ClaimsPrincipal(claimsIdentity));
                }
            }
            else
            {
                _logger?.LogWarning("Attempted to sign in after the response has already started.");
            }
        }
    }

    public async Task LogoutAsync()
    {
        await _httpContextAccessor.HttpContext!.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        // Notify authentication state provider
        if (_httpContextAccessor.HttpContext.RequestServices.GetService<AuthenticationStateProvider>()
            is CustomAuthenticationStateProvider authStateProvider)
        {
            authStateProvider.NotifyUserLogout();
        }
    }

    public async Task<UserEntity?> GetUserByUsernameAsync(string username)
    {
        try
        {
            var normalizedUsername = username.ToLowerInvariant().Trim();
            var response = await _tableClient.GetEntityAsync<UserEntity>("Users", normalizedUsername);
            return response.Value;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error retrieving user by username: {Username}", username);
            return null;
        }
    }

    public async Task<UserEntity?> GetUserByEmailAsync(string email)
    {
        try
        {
            var normalizedEmail = email.ToLowerInvariant().Trim();
            var users = _tableClient.QueryAsync<UserEntity>(u => u.Email == normalizedEmail);
            await foreach (var user in users)
            {
                return user;
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    public string GetCurrentUserId()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated == true)
        {
            return user.FindFirst("UserId")?.Value ?? "anonymous";
        }
        return "anonymous";
    }

    public string GetCurrentUsername()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated == true)
        {
            return user.FindFirst(ClaimTypes.Name)?.Value ?? "Anonymous";
        }
        return "Anonymous";
    }

    public string GetCurrentDisplayName()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated == true)
        {
            return user.FindFirst("DisplayName")?.Value ?? GetCurrentUsername();
        }
        return "Anonymous";
    }

    public bool IsAuthenticated()
    {
        return _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true;
    }

    private string GenerateSalt()
    {
        var saltBytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(saltBytes);
        }
        return Convert.ToBase64String(saltBytes);
    }

    private string HashPassword(string password, string salt)
    {
        using (var sha256 = SHA256.Create())
        {
            var saltedPassword = password + salt;
            var saltedPasswordBytes = Encoding.UTF8.GetBytes(saltedPassword);
            var hashBytes = sha256.ComputeHash(saltedPasswordBytes);
            return Convert.ToBase64String(hashBytes);
        }
    }
}
