namespace BAD_PROJECT_PWMANAGER.ViewModels.Admin;

public class AdminDashboardViewModel
{
    public IEnumerable<UserListViewModel> Users { get; set; } = [];

    public List<AuditLogListItemViewModel> RecentAuditLogs { get; set; } = new();

    public int TotalUsers { get; set; }

    public int AdminUsers { get; set; }

    public int PremiumUsers { get; set; }

    public int FreeUsers { get; set; }

    public int BannedUsers { get; set; }

    public int VaultCount { get; set; }

    public int PasswordCount { get; set; }

    public int ExportCount { get; set; }

    public int AuditLogsLast7Days { get; set; }

    public int AuditCreateCount { get; set; }

    public int AuditUpdateCount { get; set; }

    public int AuditDeleteCount { get; set; }

    public int LoginAttemptsLast7Days { get; set; }

    public int FailedLoginAttemptsLast7Days { get; set; }

    public List<LoginAttemptListItemViewModel> RecentLoginAttempts { get; set; } = new();
}
