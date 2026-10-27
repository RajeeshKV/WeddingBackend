namespace WeddingInvite.Domain.Entities;

public sealed class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AdminUserId { get; set; }
    public AdminUser AdminUser { get; set; } = null!;
    public string TokenHash { get; set; } = string.Empty;
    public Guid FamilyId { get; set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public DateTimeOffset? RevokedAtUtc { get; set; }
    public Guid? ReplacedByTokenId { get; set; }
    public string? CreatedByIpHash { get; set; }
    public string? RevocationReason { get; set; }
    public bool IsActive => RevokedAtUtc is null && ExpiresAtUtc > DateTimeOffset.UtcNow;
}
