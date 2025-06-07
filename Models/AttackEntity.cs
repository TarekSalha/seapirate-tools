using Azure;
using Azure.Data.Tables;
using SEAPIRATE.Data;

namespace SEAPIRATE.Models;

public class AttackEntity : ITableEntity
{
    public string PartitionKey { get; set; } = default!;
    public string RowKey { get; set; } = default!;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    // Attack properties
    public string TargetIsland { get; set; } = default!;
    public string TargetPlayer { get; set; } = default!;
    public int ExpectedResources { get; set; }
    public DateTime SuggestedTime { get; set; }
    public string Status { get; set; } = default!;
    public string? Notes { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? FightReportId { get; set; }
    public int? ActualResourcesGained { get; set; }

    public AttackEntity() { }

    public AttackEntity(AttackDto attack, string userId)
    {
        PartitionKey = userId;
        RowKey = attack.Id.ToString();
        TargetIsland = attack.TargetIsland;
        TargetPlayer = attack.TargetPlayer;
        ExpectedResources = attack.ExpectedResources;
        SuggestedTime = attack.SuggestedTime;
        Status = attack.Status.ToString();
        Notes = attack.Notes;
        StartedAt = attack.StartedAt;
        CompletedAt = attack.CompletedAt;
        FightReportId = attack.FightReportId;
        ActualResourcesGained = attack.ActualResourcesGained;
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
            Notes = Notes,
            StartedAt = StartedAt,
            CompletedAt = CompletedAt,
            FightReportId = FightReportId,
            ActualResourcesGained = ActualResourcesGained
        };
    }
}
