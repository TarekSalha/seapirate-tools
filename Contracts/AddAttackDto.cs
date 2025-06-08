namespace SEAPIRATE.Data;

using System.ComponentModel.DataAnnotations;

public class AddAttackDto
{
    [Required(ErrorMessage = "Target island is required")]
    [RegularExpression(@"^\d+:\d+:\d+$", ErrorMessage = "Island format should be x:y:z (e.g., 8:78:2)")]
    public string TargetIsland { get; set; } = string.Empty;
        
    public string? Notes { get; set; }
}
