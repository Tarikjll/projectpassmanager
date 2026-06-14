namespace Domain.Entities;

public class LoginAttempt : BaseEntity
{
    public string LoginIdentifier { get; set; } = string.Empty;

    public string? UserId { get; set; }

    public bool Succeeded { get; set; }

    public string? FailureReason { get; set; }

    public string? IpAddress { get; set; }

    public DateTime AttemptedAtUtc { get; set; } = DateTime.UtcNow;
}
