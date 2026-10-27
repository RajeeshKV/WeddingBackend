using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using WeddingInvite.Application.DTOs;
using WeddingInvite.Application.Interfaces;
using WeddingInvite.Domain.Entities;
using WeddingInvite.Infrastructure.Persistence;

namespace WeddingInvite.Api.Controllers;

[ApiController]
[Route("api/v1/public/weddings")]
public sealed class WeddingPublicController(
    WeddingInviteDbContext dbContext,
    IMemoryCache memoryCache,
    ICacheService cacheService) : ControllerBase
{
    [HttpGet("{slug}/config")]
    public async Task<IActionResult> GetConfig(string slug, CancellationToken cancellationToken)
    {
        var wedding = await dbContext.Weddings.AsNoTracking().FirstOrDefaultAsync(w => w.Slug == slug && w.IsPublished, cancellationToken);
        if (wedding is null) return NotFound();
        var key = cacheService.GetWeddingPublicConfigKey(wedding.Id);
        if (!memoryCache.TryGetValue(key, out PublicWeddingConfigDto? cached))
        {
            cached = new PublicWeddingConfigDto(wedding.Id, wedding.Name, wedding.Slug, wedding.EventDateUtc, wedding.Venue, wedding.Description, wedding.HeroImageUrl, wedding.RsvpEnabled, wedding.GuestUploadsEnabled);
            memoryCache.Set(key, cached, TimeSpan.FromMinutes(10));
        }
        return Ok(cached);
    }

    [HttpGet("{slug}/events")]
    public async Task<IActionResult> GetEvents(string slug, CancellationToken cancellationToken)
    {
        var wedding = await dbContext.Weddings.AsNoTracking().FirstOrDefaultAsync(w => w.Slug == slug && w.IsPublished, cancellationToken);
        if (wedding is null) return NotFound();
        var key = cacheService.GetWeddingEventsKey(wedding.Id);
        if (!memoryCache.TryGetValue(key, out List<WeddingEventDto>? cached))
        {
            cached = await dbContext.WeddingEvents
                .Where(e => e.WeddingId == wedding.Id)
                .OrderBy(e => e.DisplayOrder)
                .Select(e => new WeddingEventDto(e.Id, e.Name, e.Description, e.StartTimeUtc, e.EndTimeUtc, e.Venue, e.Address, e.DisplayOrder))
                .ToListAsync(cancellationToken);
            memoryCache.Set(key, cached, TimeSpan.FromMinutes(10));
        }
        return Ok(cached);
    }

    [HttpGet("{slug}/gallery")]
    public async Task<IActionResult> GetGallery(string slug, CancellationToken cancellationToken)
    {
        var wedding = await dbContext.Weddings.AsNoTracking().FirstOrDefaultAsync(w => w.Slug == slug && w.IsPublished, cancellationToken);
        if (wedding is null) return NotFound();
        var key = cacheService.GetWeddingGalleryKey(wedding.Id, "public");
        if (!memoryCache.TryGetValue(key, out List<PhotoDto>? cached))
        {
            cached = await dbContext.Photos
                .Where(p => p.WeddingId == wedding.Id && p.Status == PhotoStatus.Approved)
                .OrderByDescending(p => p.UploadedAtUtc)
                .Select(p => new PhotoDto(p.Id, p.ImageUrl, p.ThumbnailUrl, p.UploadedByName, p.Caption, p.Status.ToString(), p.UploadedAtUtc))
                .ToListAsync(cancellationToken);
            memoryCache.Set(key, cached, TimeSpan.FromMinutes(5));
        }
        return Ok(cached);
    }

    [HttpPost("{slug}/rsvp")]
    public async Task<IActionResult> SubmitRsvp(string slug, RsvpRequest request, CancellationToken cancellationToken)
    {
        var wedding = await dbContext.Weddings.FirstOrDefaultAsync(w => w.Slug == slug && w.IsPublished && w.RsvpEnabled, cancellationToken);
        if (wedding is null) return NotFound();
        var rsvp = new Rsvp
        {
            WeddingId = wedding.Id,
            GuestName = request.GuestName,
            Email = request.Email,
            Phone = request.Phone,
            Status = Enum.TryParse<RsvpStatus>(request.Status, out var status) ? status : RsvpStatus.Pending,
            GuestCount = request.GuestCount,
            Message = request.Message
        };
        dbContext.Rsvps.Add(rsvp);
        await dbContext.SaveChangesAsync(cancellationToken);
        cacheService.RemoveWedding(wedding.Id.ToString());
        return Ok(new RsvpDto(rsvp.Id, rsvp.GuestName, rsvp.Email, rsvp.Phone, rsvp.Status.ToString(), rsvp.GuestCount, rsvp.Message, rsvp.SubmittedAtUtc));
    }

    [HttpPost("{slug}/photos/upload")]
    public async Task<IActionResult> UploadPhotos(string slug, [FromForm] List<IFormFile> files, [FromForm] string? uploadedByName, CancellationToken cancellationToken)
    {
        var wedding = await dbContext.Weddings.FirstOrDefaultAsync(w => w.Slug == slug && w.IsPublished && w.GuestUploadsEnabled, cancellationToken);
        if (wedding is null) return NotFound();
        if (files == null || files.Count == 0) return BadRequest("No files uploaded");
        
        // This would use Cloudinary service - simplified for implementation
        var results = new List<object>();
        foreach (var file in files)
        {
            var photo = new Photo
            {
                WeddingId = wedding.Id,
                CloudinaryPublicId = Guid.NewGuid().ToString(),
                ImageUrl = "https://example.com/" + file.FileName,
                UploadedByName = uploadedByName,
                Status = wedding.AutoApprovePhotos ? PhotoStatus.Approved : PhotoStatus.Pending
            };
            dbContext.Photos.Add(photo);
            results.Add(new { id = photo.Id, status = photo.Status.ToString() });
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        cacheService.RemoveWedding(wedding.Id.ToString());
        return Ok(results);
    }
}
