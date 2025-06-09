using Microsoft.AspNetCore.Authentication.Cookies;
using SEAPIRATE.Services;
using SEAPIRATE.Repositories;
using Microsoft.AspNetCore.Components.Authorization;
using System;
using Azure.Data.Tables;
using Microsoft.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Add our custom services
builder.Services.AddScoped<AuthenticationService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<AttackRepository>();
builder.Services.AddScoped<AttackService>();
builder.Services.AddScoped<FightReportService>();

// Add authentication services
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
    });
builder.Services.AddHttpContextAccessor();
builder.Services.AddAuthorization();

// Add custom authentication state provider
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();

// Register TableClients for each table
builder.Services.AddSingleton<TableClient>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("AzureStorage") 
        ?? configuration["AzureStorage:ConnectionString"];
    return new TableClient(connectionString, "attacks");
});

builder.Services.AddSingleton<TableClient>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("AzureStorage") 
        ?? configuration["AzureStorage:ConnectionString"];
    return new TableClient(connectionString, "fightreports");
});

// Repeat for other tables as needed (e.g., Users)
builder.Services.AddSingleton<TableClientFactory>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Add authentication middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

// Map API controllers
app.MapControllers();

app.Run();
