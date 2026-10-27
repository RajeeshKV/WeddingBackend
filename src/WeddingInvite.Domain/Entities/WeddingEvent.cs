namespace WeddingInvite.Domain.Entities;

public sealed class WeddingEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WeddingId { get; set; }
    public Wedding Wedding { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTimeOffset StartTimeUtc { get; set; }
    public DateTimeOffset? EndTimeUtc { get; set; }
    public string? Venue { get; set; }
    public string? Address { get; set; }
    public int DisplayOrder { get; set; } = 0;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
