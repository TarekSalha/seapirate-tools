namespace InselkampfTools.Website.Data;
using System.ComponentModel.DataAnnotations;

public class FightReportDto
{
    [Required(ErrorMessage = "Please enter some text.")]
    public string InputText { get; set; } = null!;
    [Required(ErrorMessage = "Please select a language.")]
    public string SelectedLanguage { get; set; } = null!;
}

public enum Languages
{
    English = 1,
    German = 2
}
