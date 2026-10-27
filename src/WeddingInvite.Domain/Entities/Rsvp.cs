namespace WeddingInvite.Domain.Entities;

public enum RsvpStatus
{
    Pending = 0,
    Attending = 1,
    NotAttending = 2,
    Maybe = 3
}

public sealed class Rsvp
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WeddingId { get; set; }
    public Wedding Wedding { get; set; } = null!;
    public string GuestName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public RsvpStatus Status { get; set; } = RsvpStatus.Pending;
    public int GuestCount { get; set; } = 1;
    public string? Message { get; set; }
    public DateTimeOffset SubmittedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
