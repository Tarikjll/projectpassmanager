namespace BAD_PROJECT_PWMANAGER.ViewModels.Admin;

public class LoginAttemptListItemViewModel
{
    public int Id { get; set; }

    public string LoginIdentifier { get; set; } = string.Empty;

    public bool Succeeded { get; set; }

    public string? FailureReason { get; set; }

    public string? IpAddress { get; set; }

    public DateTime AttemptedAtUtc { get; set; }
}
