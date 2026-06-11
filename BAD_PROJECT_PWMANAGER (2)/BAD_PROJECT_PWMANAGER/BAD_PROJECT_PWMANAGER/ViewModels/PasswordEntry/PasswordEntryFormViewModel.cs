using System.ComponentModel.DataAnnotations;

namespace BAD_PROJECT_PWMANAGER.ViewModels.PasswordEntry;

public class PasswordEntryFormViewModel
{
    public int Id { get; set; }

    [Required]
    public int VaultId { get; set; }

    [Required(ErrorMessage = "Platform is verplicht.")]
    [StringLength(100, ErrorMessage = "Platform mag maximaal 100 tekens bevatten.")]
    [Display(Name = "Platform / website")]
    public string Platform { get; set; } = string.Empty;

    [Required(ErrorMessage = "Gebruikersnaam is verplicht.")]
    [StringLength(100, ErrorMessage = "Gebruikersnaam mag maximaal 100 tekens bevatten.")]
    [Display(Name = "Gebruikersnaam / e-mail")]
    public string Username { get; set; } = string.Empty;

    [Url(ErrorMessage = "Geef een geldige URL in.")]
    [Display(Name = "Website URL")]
    public string? Url { get; set; }

    [Required(ErrorMessage = "Wachtwoord is verplicht.")]
    [DataType(DataType.Password)]
    [Display(Name = "Wachtwoord")]
    public string PlainPassword { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Notities mogen maximaal 500 tekens bevatten.")]
    [Display(Name = "Notities")]
    public string? Notes { get; set; }
}