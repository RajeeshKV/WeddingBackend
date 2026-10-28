using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WeddingInvite.Api.Helpers;
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
    private Guid GetTenantId() => JwtHelper.GetTenantId(HttpContext);

    private async Task<Wedding?> GetTenantWeddingAsync(CancellationToken cancellationToken)
    {
        var tenantId = GetTenantId();
        return await dbContext.Weddings.FirstOrDefaultAsync(w => w.AdminUserId == tenantId, cancellationToken);
    }

    [HttpGet("wedding/dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
    {
        var wedding = await GetTenantWeddingAsync(cancellationToken);
        if (wedding is null) return NotFound();
        var weddingId = wedding.Id;
        var totalRsvps = await dbContext.Rsvps.CountAsync(r => r.WeddingId == weddingId, cancellationToken);
        var attending = await dbContext.Rsvps.CountAsync(r => r.WeddingId == weddingId && r.Status == RsvpStatus.Attending, cancellationToken);
        var notAttending = await dbContext.Rsvps.CountAsync(r => r.WeddingId == weddingId && r.Status == RsvpStatus.NotAttending, cancellationToken);
        var maybe = await dbContext.Rsvps.CountAsync(r => r.WeddingId == weddingId && r.Status == RsvpStatus.Maybe, cancellationToken);
        var totalPhotos = await dbContext.Photos.CountAsync(p => p.WeddingId == weddingId, cancellationToken);
        var pendingPhotos = await dbContext.Photos.CountAsync(p => p.WeddingId == weddingId && p.Status == PhotoStatus.Pending, cancellationToken);
        var approvedPhotos = await dbContext.Photos.CountAsync(p => p.WeddingId == weddingId && p.Status == PhotoStatus.Approved, cancellationToken);
        return Ok(new AdminDashboardDto(totalRsvps, attending, notAttending, maybe, totalPhotos, pendingPhotos, approvedPhotos));
    }

    [HttpGet("wedding/config")]
    public async Task<IActionResult> GetConfig(CancellationToken cancellationToken)
    {
        var wedding = await GetTenantWeddingAsync(cancellationToken);
        if (wedding is null) return NotFound();
        return Ok(new PublicWeddingConfigDto(wedding.Id, wedding.Name, wedding.Slug, wedding.EventDateUtc, wedding.Venue, wedding.Description, wedding.HeroImageUrl, wedding.RsvpEnabled, wedding.GuestUploadsEnabled));
    }

    [HttpPut("wedding/config")]
    public async Task<IActionResult> UpdateConfig(UpdateWeddingConfigRequest request, CancellationToken cancellationToken)
    {
        var wedding = await GetTenantWeddingAsync(cancellationToken);
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
        cacheService.RemoveWedding(wedding.Id.ToString());
        return NoContent();
    }

    [HttpGet("wedding/photos")]
    public async Task<IActionResult> GetPhotos([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;
        var wedding = await GetTenantWeddingAsync(cancellationToken);
        if (wedding is null) return NotFound();
        var query = dbContext.Photos.Where(p => p.WeddingId == wedding.Id).OrderByDescending(p => p.UploadedAtUtc);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(p => new AdminPhotoDto(p.Id, p.ImageUrl, p.ThumbnailUrl, p.UploadedByName, p.Caption, p.Status.ToString(), p.UploadedAtUtc, p.WeddingId))
            .ToListAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return Ok(new PagedResult<AdminPhotoDto>(items, page, pageSize, totalCount, totalPages));
    }

    [HttpGet("photos")]
    public async Task<IActionResult> GetAllPhotos([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;
        var tenantId = GetTenantId();
        var query = dbContext.Photos.Where(p => p.Wedding!.AdminUserId == tenantId).OrderByDescending(p => p.UploadedAtUtc);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(p => new AdminPhotoDto(p.Id, p.ImageUrl, p.ThumbnailUrl, p.UploadedByName, p.Caption, p.Status.ToString(), p.UploadedAtUtc, p.WeddingId))
            .ToListAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return Ok(new PagedResult<AdminPhotoDto>(items, page, pageSize, totalCount, totalPages));
    }

    [HttpGet("photos/pending")]
    public async Task<IActionResult> GetPendingPhotos([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;
        var tenantId = GetTenantId();
        var query = dbContext.Photos.Where(p => p.Status == PhotoStatus.Pending && p.Wedding!.AdminUserId == tenantId).OrderBy(p => p.UploadedAtUtc);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(p => new AdminPhotoDto(p.Id, p.ImageUrl, p.ThumbnailUrl, p.UploadedByName, p.Caption, p.Status.ToString(), p.UploadedAtUtc, p.WeddingId))
            .ToListAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return Ok(new PagedResult<AdminPhotoDto>(items, page, pageSize, totalCount, totalPages));
    }

    [HttpPost("photos/{photoId}/approve")]
    public async Task<IActionResult> ApprovePhoto(Guid photoId, CancellationToken cancellationToken)
    {
        var tenantId = GetTenantId();
        var photo = await dbContext.Photos.Include(p => p.Wedding).FirstOrDefaultAsync(p => p.Id == photoId && p.Wedding!.AdminUserId == tenantId, cancellationToken);
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
        var tenantId = GetTenantId();
        var photo = await dbContext.Photos.Include(p => p.Wedding).FirstOrDefaultAsync(p => p.Id == photoId && p.Wedding!.AdminUserId == tenantId, cancellationToken);
        if (photo is null) return NotFound();
        photo.Status = PhotoStatus.Rejected;
        photo.ReviewedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        cacheService.RemoveWedding(photo.WeddingId.ToString());
        return NoContent();
    }

    [HttpDelete("photos/{photoId}")]
    public async Task<IActionResult> DeletePhoto(Guid photoId, CancellationToken cancellationToken)
    {
        var tenantId = GetTenantId();
        var photo = await dbContext.Photos.Include(p => p.Wedding).FirstOrDefaultAsync(p => p.Id == photoId && p.Wedding!.AdminUserId == tenantId, cancellationToken);
        if (photo is null) return NotFound();
        dbContext.Photos.Remove(photo);
        await dbContext.SaveChangesAsync(cancellationToken);
        cacheService.RemoveWedding(photo.WeddingId.ToString());
        return NoContent();
    }

    [HttpPost("photos/bulk-approve")]
    public async Task<IActionResult> BulkApprove([FromBody] List<Guid> photoIds, CancellationToken cancellationToken)
    {
        var tenantId = GetTenantId();
        var photos = await dbContext.Photos.Include(p => p.Wedding).Where(p => photoIds.Contains(p.Id) && p.Wedding!.AdminUserId == tenantId).ToListAsync(cancellationToken);
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

    [HttpPost("wedding/photos/auto-approve")]
    public async Task<IActionResult> EnableAutoApprove(CancellationToken cancellationToken)
    {
        var wedding = await GetTenantWeddingAsync(cancellationToken);
        if (wedding is null) return NotFound();
        wedding.AutoApprovePhotos = true;
        wedding.UpdatedAtUtc = DateTimeOffset.UtcNow;
        var pendingPhotos = await dbContext.Photos.Where(p => p.WeddingId == wedding.Id && p.Status == PhotoStatus.Pending).ToListAsync(cancellationToken);
        foreach (var photo in pendingPhotos)
        {
            photo.Status = PhotoStatus.Approved;
            photo.ReviewedAtUtc = DateTimeOffset.UtcNow;
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        cacheService.RemoveWedding(wedding.Id.ToString());
        return NoContent();
    }

    // Backward compatibility
    [HttpGet("weddings/{weddingId}/dashboard")]
    public async Task<IActionResult> GetDashboardLegacy(Guid weddingId, CancellationToken cancellationToken) => await GetDashboard(cancellationToken);

    [HttpGet("weddings/{weddingId}/config")]
    public async Task<IActionResult> GetConfigLegacy(Guid weddingId, CancellationToken cancellationToken) => await GetConfig(cancellationToken);

    [HttpPut("weddings/{weddingId}/config")]
    public async Task<IActionResult> UpdateConfigLegacy(Guid weddingId, UpdateWeddingConfigRequest request, CancellationToken cancellationToken) => await UpdateConfig(request, cancellationToken);

    [HttpGet("weddings/{weddingId}/photos")]
    public async Task<IActionResult> GetPhotosLegacy(Guid weddingId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default) => await GetPhotos(page, pageSize, cancellationToken);

    [HttpPost("weddings/{weddingId}/photos/auto-approve")]
    public async Task<IActionResult> EnableAutoApproveLegacy(Guid weddingId, CancellationToken cancellationToken) => await EnableAutoApprove(cancellationToken);
}
