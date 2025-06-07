namespace SEAPIRATE.Data;
using System.ComponentModel.DataAnnotations;

public class FightReportDto
{
    [Required(ErrorMessage = "Please enter the fight report text")]
    [MinLength(10, ErrorMessage = "Fight report seems too short")]
    public string InputText { get; set; } = null!;
    [Required(ErrorMessage = "Please select a language.")]
    public string SelectedLanguage { get; set; } = null!;
}

public enum Languages
{
    English = 1,
    German = 2
}
