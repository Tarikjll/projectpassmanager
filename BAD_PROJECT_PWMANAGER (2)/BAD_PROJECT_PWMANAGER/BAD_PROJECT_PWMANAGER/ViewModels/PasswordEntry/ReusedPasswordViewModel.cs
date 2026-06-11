namespace BAD_PROJECT_PWMANAGER.ViewModels.PasswordEntry
{
    public class ReusedPasswordViewModel
    {
        public List<ReusedPasswordGroupViewModel> Groups { get; set; } = new();
    }

    public class ReusedPasswordGroupViewModel
    {
        public int ReuseCount { get; set; }

        public List<ReusedPasswordItemViewModel> Items { get; set; } = new();
    }

    public class ReusedPasswordItemViewModel
    {
        public int PasswordEntryId { get; set; }

        public int VaultId { get; set; }

        public string Platform { get; set; } = string.Empty;

        public string Username { get; set; } = string.Empty;

        public string? Url { get; set; }
    }
}