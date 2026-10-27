namespace WeddingInvite.Domain.Entities;

public sealed class Wedding
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AdminUserId { get; set; }
    public AdminUser AdminUser { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public DateTimeOffset? EventDateUtc { get; set; }
    public string? Venue { get; set; }
    public string? Description { get; set; }
    public string? HeroImageUrl { get; set; }
    public bool IsPublished { get; set; } = true;
    public bool RsvpEnabled { get; set; } = true;
    public bool GuestUploadsEnabled { get; set; } = true;
    public bool AutoApprovePhotos { get; set; } = false;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    
    public ICollection<WeddingEvent> Events { get; set; } = new List<WeddingEvent>();
    public ICollection<Photo> Photos { get; set; } = new List<Photo>();
    public ICollection<Rsvp> Rsvps { get; set; } = new List<Rsvp>();
}
