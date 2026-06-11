namespace BAD_PROJECT_PWMANAGER.ViewModels.Vault;

public class VaultListItemViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }
}