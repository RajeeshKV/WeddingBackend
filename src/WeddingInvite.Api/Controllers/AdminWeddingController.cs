using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WeddingInvite.Application.DTOs;
using WeddingInvite.Application.Interfaces;
using WeddingInvite.Domain.Entities;
using WeddingInvite.Infrastructure.Persistence;

namespace WeddingInvite.Api.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/v1/admin")]
public sealed class AdminWeddingController(
    WeddingInviteDbContext dbContext,
    ICacheService cacheService) : ControllerBase
{
    [HttpGet("weddings/{weddingId}/dashboard")]
    public async Task<AdminDashboardDto> GetDashboard(Guid weddingId, CancellationToken cancellationToken)
    {
        var totalRsvps = await dbContext.Rsvps.CountAsync(r => r.WeddingId == weddingId, cancellationToken);
        var attending = await dbContext.Rsvps.CountAsync(r => r.WeddingId == weddingId && r.Status == RsvpStatus.Attending, cancellationToken);
        var notAttending = await dbContext.Rsvps.CountAsync(r => r.WeddingId == weddingId && r.Status == RsvpStatus.NotAttending, cancellationToken);
        var maybe = await dbContext.Rsvps.CountAsync(r => r.WeddingId == weddingId && r.Status == RsvpStatus.Maybe, cancellationToken);
        var totalPhotos = await dbContext.Photos.CountAsync(p => p.WeddingId == weddingId, cancellationToken);
        var pendingPhotos = await dbContext.Photos.CountAsync(p => p.WeddingId == weddingId && p.Status == PhotoStatus.Pending, cancellationToken);
        var approvedPhotos = await dbContext.Photos.CountAsync(p => p.WeddingId == weddingId && p.Status == PhotoStatus.Approved, cancellationToken);
        return new AdminDashboardDto(totalRsvps, attending, notAttending, maybe, totalPhotos, pendingPhotos, approvedPhotos);
    }

    [HttpPut("weddings/{weddingId}/config")]
    public async Task<IActionResult> UpdateConfig(Guid weddingId, UpdateWeddingConfigRequest request, CancellationToken cancellationToken)
    {
        var wedding = await dbContext.Weddings.FindAsync(new object[] { weddingId }, cancellationToken);
        if (wedding is null) return NotFound();
        if (request.Name != null) wedding.Name = request.Name;
        if (request.Description != null) wedding.Description = request.Description;
        if (request.Venue != null) wedding.Venue = request.Venue;
        if (request.EventDateUtc.HasValue) wedding.EventDateUtc = request.EventDateUtc;
        if (request.HeroImageUrl != null) wedding.HeroImageUrl = request.HeroImageUrl;
        if (request.RsvpEnabled.HasValue) wedding.RsvpEnabled = request.RsvpEnabled.Value;
        if (request.GuestUploadsEnabled.HasValue) wedding.GuestUploadsEnabled = request.GuestUploadsEnabled.Value;
        if (request.AutoApprovePhotos.HasValue) wedding.AutoApprovePhotos = request.AutoApprovePhotos.Value;
        wedding.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        cacheService.RemoveWedding(weddingId.ToString());
        return NoContent();
    }

    [HttpGet("photos/pending")]
    public async Task<IActionResult> GetPendingPhotos(CancellationToken cancellationToken)
    {
        var photos = await dbContext.Photos
            .Where(p => p.Status == PhotoStatus.Pending)
            .OrderBy(p => p.UploadedAtUtc)
            .Select(p => new AdminPhotoDto(p.Id, p.ImageUrl, p.ThumbnailUrl, p.UploadedByName, p.Caption, p.Status.ToString(), p.UploadedAtUtc, p.WeddingId))
            .ToListAsync(cancellationToken);
        return Ok(photos);
    }

    [HttpPost("photos/{photoId}/approve")]
    public async Task<IActionResult> ApprovePhoto(Guid photoId, CancellationToken cancellationToken)
    {
        var photo = await dbContext.Photos.FindAsync(new object[] { photoId }, cancellationToken);
        if (photo is null) return NotFound();
        photo.Status = PhotoStatus.Approved;
        photo.ReviewedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        cacheService.RemoveWedding(photo.WeddingId.ToString());
        return NoContent();
    }

    [HttpPost("photos/{photoId}/reject")]
    public async Task<IActionResult> RejectPhoto(Guid photoId, CancellationToken cancellationToken)
    {
        var photo = await dbContext.Photos.FindAsync(new object[] { photoId }, cancellationToken);
        if (photo is null) return NotFound();
        photo.Status = PhotoStatus.Rejected;
        photo.ReviewedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        cacheService.RemoveWedding(photo.WeddingId.ToString());
        return NoContent();
    }

    [HttpPost("photos/bulk-approve")]
    public async Task<IActionResult> BulkApprove([FromBody] List<Guid> photoIds, CancellationToken cancellationToken)
    {
        var photos = await dbContext.Photos.Where(p => photoIds.Contains(p.Id)).ToListAsync(cancellationToken);
        foreach (var photo in photos)
        {
            photo.Status = PhotoStatus.Approved;
            photo.ReviewedAtUtc = DateTimeOffset.UtcNow;
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        foreach (var photo in photos)
        {
            cacheService.RemoveWedding(photo.WeddingId.ToString());
        }
        return NoContent();
    }

    [HttpPost("weddings/{weddingId}/photos/auto-approve")]
    public async Task<IActionResult> EnableAutoApprove(Guid weddingId, CancellationToken cancellationToken)
    {
        var wedding = await dbContext.Weddings.FindAsync(new object[] { weddingId }, cancellationToken);
        if (wedding is null) return NotFound();
        wedding.AutoApprovePhotos = true;
        wedding.UpdatedAtUtc = DateTimeOffset.UtcNow;
        var pendingPhotos = await dbContext.Photos.Where(p => p.WeddingId == weddingId && p.Status == PhotoStatus.Pending).ToListAsync(cancellationToken);
        foreach (var photo in pendingPhotos)
        {
            photo.Status = PhotoStatus.Approved;
            photo.ReviewedAtUtc = DateTimeOffset.UtcNow;
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        cacheService.RemoveWedding(weddingId.ToString());
        return NoContent();
    }
}
