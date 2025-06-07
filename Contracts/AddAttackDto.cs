namespace SEAPIRATE.Data;

using System.ComponentModel.DataAnnotations;

public class AddAttackDto
{
    [Required(ErrorMessage = "Target island is required")]
    [RegularExpression(@"^\d+:\d+$", ErrorMessage = "Island format should be like '12:345'")]
    public string TargetIsland { get; set; } = null!;
    
    [Required(ErrorMessage = "Target player is required")]
    public string TargetPlayer { get; set; } = null!;
    
    [Required(ErrorMessage = "Expected resources is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Expected resources must be greater than 0")]
    public int ExpectedResources { get; set; }
    
    [Required(ErrorMessage = "Suggested time is required")]
    public DateTime SuggestedTime { get; set; } = DateTime.Now.AddHours(1);
    
    public string? Notes { get; set; }
}
