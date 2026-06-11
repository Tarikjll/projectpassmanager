using System.ComponentModel.DataAnnotations;

namespace BAD_PROJECT_PWMANAGER.ViewModels.Vault;

public class VaultFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Naam is verplicht.")]
    [StringLength(100, ErrorMessage = "Naam mag maximaal 100 tekens bevatten.")]
    [Display(Name = "Naam")]
    public string Name { get; set; } = string.Empty;

    [StringLength(250, ErrorMessage = "Beschrijving mag maximaal 250 tekens bevatten.")]
    [Display(Name = "Beschrijving")]
    public string? Description { get; set; }
}