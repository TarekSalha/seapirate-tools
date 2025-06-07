using Azure;
using Azure.Data.Tables;
using SEAPIRATE.Data;

namespace SEAPIRATE.Models;

public class AttackEntity : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty;
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public Azure.ETag ETag { get; set; }

    // Attack properties
    public string Id { get; set; } = string.Empty;
    public string TargetIsland { get; set; } = string.Empty;
    public string TargetPlayer { get; set; } = string.Empty;
    public int ExpectedResources { get; set; }
    public DateTime SuggestedTime { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string? FightReportId { get; set; }
    public int? ActualResourcesGained { get; set; }
    public DateTime CreatedAt { get; set; }

    public AttackEntity()
    {
        // Required for Azure Table Storage
    }

    public AttackEntity(AttackDto attack, string userId)
    {
        PartitionKey = userId;
        RowKey = attack.Id.ToString();
        Id = attack.Id.ToString();
        TargetIsland = attack.TargetIsland;
        TargetPlayer = attack.TargetPlayer;
        ExpectedResources = attack.ExpectedResources;
        
        // Convert all DateTime values to UTC to avoid Azure SDK issues
        SuggestedTime = attack.SuggestedTime.Kind == DateTimeKind.Utc 
            ? attack.SuggestedTime 
            : DateTime.SpecifyKind(attack.SuggestedTime, DateTimeKind.Utc);
            
        StartedAt = attack.StartedAt?.Kind == DateTimeKind.Utc 
            ? attack.StartedAt 
            : attack.StartedAt.HasValue 
                ? DateTime.SpecifyKind(attack.StartedAt.Value, DateTimeKind.Utc) 
                : null;
                
        CompletedAt = attack.CompletedAt?.Kind == DateTimeKind.Utc 
            ? attack.CompletedAt 
            : attack.CompletedAt.HasValue 
                ? DateTime.SpecifyKind(attack.CompletedAt.Value, DateTimeKind.Utc) 
                : null;
                
        Status = attack.Status.ToString();
        Notes = attack.Notes;
        FightReportId = attack.FightReportId;
        ActualResourcesGained = attack.ActualResourcesGained;
        
        // Set CreatedAt to current UTC time
        CreatedAt = DateTime.UtcNow;
    }

    public AttackDto ToDto()
    {
        return new AttackDto
        {
            Id = Guid.Parse(Id),
            TargetIsland = TargetIsland,
            TargetPlayer = TargetPlayer,
            ExpectedResources = ExpectedResources,
            SuggestedTime = SuggestedTime,
            StartedAt = StartedAt,
            CompletedAt = CompletedAt,
            Status = Enum.Parse<AttackStatus>(Status),
            Notes = Notes,
            FightReportId = FightReportId,
            ActualResourcesGained = ActualResourcesGained
        };
    }
}