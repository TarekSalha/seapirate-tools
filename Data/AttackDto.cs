using System.ComponentModel.DataAnnotations;

namespace SEAPIRATE.Website.Data;

public class AttackDto
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public string TargetIsland { get; set; } = null!;
    
    [Required]
    public string TargetPlayer { get; set; } = null!;
    
    [Range(0, int.MaxValue)]
    public int ExpectedResources { get; set; }
    
    public DateTime SuggestedTime { get; set; } = DateTime.Now;
    
    public AttackStatus Status { get; set; } = AttackStatus.Suggested;
    
    public string? Notes { get; set; }
    
    public DateTime? StartedAt { get; set; }
    
    public DateTime? CompletedAt { get; set; }
    
    public string? FightReportId { get; set; }
    
    public int? ActualResourcesGained { get; set; }
}

public enum AttackStatus
{
    Suggested,
    Pending,
    Completed,
    Failed
}
