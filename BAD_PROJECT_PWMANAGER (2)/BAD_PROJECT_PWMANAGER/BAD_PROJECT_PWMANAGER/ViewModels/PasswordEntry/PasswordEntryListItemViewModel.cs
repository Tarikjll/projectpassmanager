namespace BAD_PROJECT_PWMANAGER.ViewModels.PasswordEntry;

public class PasswordEntryListItemViewModel
{
    public int Id { get; set; }

    public int VaultId { get; set; }

    public string Platform { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string? Url { get; set; }

    public string StrengthLabel { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public bool IsPasswordReused { get; set; }

    public List<string> ReusedWithPlatforms { get; set; } = new();

    public bool IsPasswordOld { get; set; }

    public int PasswordAgeInDays { get; set; }
}