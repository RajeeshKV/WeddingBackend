namespace WeddingInvite.Application.DTOs;

public sealed record PublicWeddingConfigDto(Guid Id, string Name, string Slug, DateTimeOffset? EventDateUtc, string? Venue, string? Description, string? HeroImageUrl, bool RsvpEnabled, bool GuestUploadsEnabled);
public sealed record WeddingEventDto(Guid Id, string Name, string? Description, DateTimeOffset StartTimeUtc, DateTimeOffset? EndTimeUtc, string? Venue, string? Address, int DisplayOrder);
public sealed record PhotoDto(Guid Id, string ImageUrl, string? ThumbnailUrl, string? UploadedByName, string? Caption, string Status, DateTimeOffset UploadedAtUtc);
public sealed record AdminPhotoDto(Guid Id, string ImageUrl, string? ThumbnailUrl, string? UploadedByName, string? Caption, string Status, DateTimeOffset UploadedAtUtc, Guid WeddingId);
public sealed record RsvpRequest(string GuestName, string? Email, string? Phone, string Status, int GuestCount, string? Message);
public sealed record RsvpDto(Guid Id, string GuestName, string? Email, string? Phone, string Status, int GuestCount, string? Message, DateTimeOffset SubmittedAtUtc);
public sealed record AdminDashboardDto(int TotalRsvps, int AttendingCount, int NotAttendingCount, int MaybeCount, int TotalPhotos, int PendingPhotos, int ApprovedPhotos);
public sealed record UpdateWeddingConfigRequest(string? Name, string? Description, string? Venue, DateTimeOffset? EventDateUtc, string? HeroImageUrl, bool? RsvpEnabled, bool? GuestUploadsEnabled, bool? AutoApprovePhotos);
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount, int TotalPages);
