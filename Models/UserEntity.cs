using Azure;
using Azure.Data.Tables;
using System.ComponentModel.DataAnnotations;

namespace SEAPIRATE.Models;

public class UserEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "Users";
    public string RowKey { get; set; } = string.Empty; // Will be the username
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    [Required]
    public string Username { get; set; } = string.Empty;
    
    [Required]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    public string PasswordHash { get; set; } = string.Empty;
    
    public string DisplayName { get; set; } = string.Empty;
    
    public string GamePlayerName { get; set; } = string.Empty;
    
    public string HomeIsland { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime LastLoginAt { get; set; } = DateTime.UtcNow;
    
    public bool IsActive { get; set; } = true;

    // Salt for password hashing
    public string Salt { get; set; } = string.Empty;
}
