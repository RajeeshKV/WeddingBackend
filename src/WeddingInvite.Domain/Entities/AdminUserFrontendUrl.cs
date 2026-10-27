namespace WeddingInvite.Domain.Entities;

public sealed class AdminUserFrontendUrl
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AdminUserId { get; set; }
    public AdminUser AdminUser { get; set; } = null!;
    public string Url { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
