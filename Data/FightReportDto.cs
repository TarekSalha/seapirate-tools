using System.ComponentModel.DataAnnotations;

namespace SEAPIRATE.Website.Data;

public class FightReportDto
{
    [Required(ErrorMessage = "Please select a language")]
    public string SelectedLanguage { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Please enter the fight report text")]
    [MinLength(10, ErrorMessage = "Fight report seems too short")]
    public string InputText { get; set; } = string.Empty;
}

public enum Languages
{
    English,
    German
}
