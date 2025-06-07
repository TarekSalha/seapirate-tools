using System.ComponentModel.DataAnnotations;

namespace SEAPIRATE.Website.Data;

public class AddAttackDto
{
    [Required(ErrorMessage = "Target island is required")]
    [RegularExpression(@"\d+:\d+:\d+", ErrorMessage = "Island format should be x:y:z")]
    public string TargetIsland { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Target player name is required")]
    public string TargetPlayer { get; set; } = string.Empty;
    
    [Range(0, int.MaxValue, ErrorMessage = "Expected resources must be positive")]
    public int ExpectedResources { get; set; }
    
    public DateTime SuggestedTime { get; set; } = DateTime.Now.AddHours(1);
    
    public string? Notes { get; set; }
}
