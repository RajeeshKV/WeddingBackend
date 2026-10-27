namespace WeddingInvite.Domain.Entities;

public sealed class AdminUser
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FullName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool MustChangePassword { get; set; } = true;
    public int SecurityVersion { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    
    public ICollection<AdminUserFrontendUrl> FrontendUrls { get; set; } = new List<AdminUserFrontendUrl>();
    public ICollection<Wedding> Weddings { get; set; } = new List<Wedding>();
}
