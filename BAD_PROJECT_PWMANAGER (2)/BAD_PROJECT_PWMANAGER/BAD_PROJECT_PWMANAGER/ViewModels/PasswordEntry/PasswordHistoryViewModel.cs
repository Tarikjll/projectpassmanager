namespace BAD_PROJECT_PWMANAGER.ViewModels.PasswordEntry
{
    public class PasswordHistoryViewModel
    {
        public int PasswordEntryId { get; set; }
        public int VaultId { get; set; }

        public string Platform { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;

        public List<PasswordHistoryItemViewModel> Items { get; set; } = new();
    }

    public class PasswordHistoryItemViewModel
    {
        public int Id { get; set; }

        public string OldPassword { get; set; } = string.Empty;

        public DateTime ChangedAt { get; set; }
    }
}