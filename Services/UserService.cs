using SEAPIRATE.Models;

namespace SEAPIRATE.Services;

public class UserService
{
    private readonly AuthenticationService _authService;

    public UserService(AuthenticationService authService)
    {
        _authService = authService;
    }

    public async Task<string> GetCurrentUserIdAsync()
    {
        return _authService.GetCurrentUserId();
    }

    public async Task<string> GetCurrentUserDisplayNameAsync()
    {
        return _authService.GetCurrentDisplayName();
    }

    public bool IsAuthenticated()
    {
        return _authService.IsAuthenticated();
    }

    public async Task<UserEntity?> GetCurrentUserAsync()
    {
        if (!_authService.IsAuthenticated())
            return null;

        var username = _authService.GetCurrentUsername();
        return await _authService.GetUserByUsernameAsync(username);
    }
}
