namespace WeddingInvite.Domain.Entities;

public enum PhotoStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}

public sealed class Photo
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WeddingId { get; set; }
    public Wedding Wedding { get; set; } = null!;
    public string CloudinaryPublicId { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string? UploadedByName { get; set; }
    public string? Caption { get; set; }
    public PhotoStatus Status { get; set; } = PhotoStatus.Pending;
    public DateTimeOffset UploadedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ReviewedAtUtc { get; set; }
    public Guid? ReviewedByAdminId { get; set; }
}
