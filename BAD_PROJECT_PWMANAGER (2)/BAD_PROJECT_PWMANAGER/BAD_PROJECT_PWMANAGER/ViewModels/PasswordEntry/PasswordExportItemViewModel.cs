namespace BAD_PROJECT_PWMANAGER.ViewModels.PasswordEntry;

public class PasswordExportItemViewModel
{
    public int Id { get; set; }

    public string DestinationType { get; set; } = string.Empty;

    public string DestinationMasked { get; set; } = string.Empty;

    public DateTime ExportedAt { get; set; }
}
