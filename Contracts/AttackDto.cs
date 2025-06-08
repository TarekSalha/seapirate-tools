namespace SEAPIRATE.Data;

public class AttackDto
{
    public string Id { get; set; } = string.Empty;
    public string TargetIsland { get; set; } = string.Empty;
    public int Latitude { get; set; }
    public int Longitude { get; set; }
    public int Ocean { get; set; }
    public int IslandGroup { get; set; }
    public int IslandNumber { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public AttackStatus Status { get; set; } = AttackStatus.Suggested;
    public string? Notes { get; set; }
    public string? FightReportId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum AttackStatus
{
    Suggested,
    Pending,
    Succeeded,
    Failed
}