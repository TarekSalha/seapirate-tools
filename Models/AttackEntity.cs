using Azure;
using Azure.Data.Tables;
using SEAPIRATE.Data;
using System.Runtime.Serialization;

namespace SEAPIRATE.Models;

public class AttackEntity : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty;
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public Azure.ETag ETag { get; set; }

    // Attack properties
    public Guid Id { get; set; } = Guid.NewGuid();
    public string TargetIsland { get; set; } = string.Empty;
    public int Latitude { get; set; }
    public int Longitude { get; set; }
    public int Ocean { get; set; }
    public int IslandGroup { get; set; }
    public int IslandNumber { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string StatusString
    {
        get => Status.ToString();
        set => Status = Enum.Parse<AttackStatus>(value);
    }

    [IgnoreDataMember] // Optional, for some serializers
    public AttackStatus Status { get; set; } = AttackStatus.Suggested;
    public string? Notes { get; set; }
    public string? FightReportId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public enum AttackStatus
    {
        Suggested, // not yet executed
        Pending,   // started and currently executed
        Succeeded, // successful
        Failed     // blocked by defender
    }

    public AttackEntity()
    {
        // Required for Azure Table Storage
    }

    public AttackEntity(AttackEntity attack, string userId)
    {
        PartitionKey = userId;
        RowKey = attack.Id.ToString("N"); // Use a consistent format for RowKey
        Id = attack.Id;
        TargetIsland = attack.TargetIsland;
        Latitude = attack.Latitude;
        Longitude = attack.Longitude;
        Ocean = attack.Ocean;
        IslandGroup = attack.IslandGroup;
        IslandNumber = attack.IslandNumber;
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
        Status = attack.Status;
        Notes = attack.Notes;
        FightReportId = attack.FightReportId;
        CreatedAt = DateTime.UtcNow;
    }

    public AttackDto ToDto()
    {
        return new AttackDto
        {
            Id = Id.ToString("N"),
            TargetIsland = TargetIsland,
            StartedAt = StartedAt,
            CompletedAt = CompletedAt,
            Status = (SEAPIRATE.Data.AttackStatus)(int)Status,
            Notes = Notes,
            FightReportId = FightReportId,
        };
    }
}
