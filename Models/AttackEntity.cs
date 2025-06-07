using Azure;
using Azure.Data.Tables;
using InselkampfTools.Website.Data;

namespace InselkampfTools.Website.Models;

public class AttackEntity : ITableEntity
{
    public string PartitionKey { get; set; } = null!;
    public string RowKey { get; set; } = null!;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    // Attack-specific properties
    public string TargetIsland { get; set; } = null!;
    public string TargetPlayer { get; set; } = null!;
    public int ExpectedResources { get; set; }
    public DateTime SuggestedTime { get; set; }
    public string Status { get; set; } = AttackStatus.Suggested.ToString();
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? FightReportId { get; set; }
    public int? ActualResourcesGained { get; set; }
    public string? Notes { get; set; }

    public AttackEntity()
    {
        // Default constructor for Azure Tables
    }

    public AttackEntity(AttackDto attack, string userId = "default")
    {
        PartitionKey = userId; // Partition by user for multi-user support
        RowKey = attack.Id.ToString();
        TargetIsland = attack.TargetIsland;
        TargetPlayer = attack.TargetPlayer;
        ExpectedResources = attack.ExpectedResources;
        SuggestedTime = attack.SuggestedTime;
        Status = attack.Status.ToString();
        StartedAt = attack.StartedAt;
        CompletedAt = attack.CompletedAt;
        FightReportId = attack.FightReportId;
        ActualResourcesGained = attack.ActualResourcesGained;
        Notes = attack.Notes;
    }

    public AttackDto ToDto()
    {
        return new AttackDto
        {
            Id = Guid.Parse(RowKey),
            TargetIsland = TargetIsland,
            TargetPlayer = TargetPlayer,
            ExpectedResources = ExpectedResources,
            SuggestedTime = SuggestedTime,
            Status = Enum.Parse<AttackStatus>(Status),
            StartedAt = StartedAt,
            CompletedAt = CompletedAt,
            FightReportId = FightReportId,
            ActualResourcesGained = ActualResourcesGained,
            Notes = Notes
        };
    }
}
