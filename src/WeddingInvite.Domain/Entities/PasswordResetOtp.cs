namespace WeddingInvite.Domain.Entities;

public sealed class PasswordResetOtp
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AdminUserId { get; set; }
    public AdminUser AdminUser { get; set; } = null!;
    public string OtpHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UsedAtUtc { get; set; }
    public bool IsActive => UsedAtUtc is null && ExpiresAtUtc > DateTimeOffset.UtcNow;
}
