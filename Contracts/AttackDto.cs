namespace SEAPIRATE.Data;

public class AttackDto
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string TargetIsland { get; set; } = null!;
    public string TargetPlayer { get; set; } = null!;
    public int ExpectedResources { get; set; }
    public DateTime SuggestedTime { get; set; }
    public AttackStatus Status { get; set; } = AttackStatus.Suggested;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? FightReportId { get; set; }
    public int? ActualResourcesGained { get; set; }
    public string? Notes { get; set; }
}

public enum AttackStatus
{
    Suggested,
    Pending,
    Completed,
    Failed
}
