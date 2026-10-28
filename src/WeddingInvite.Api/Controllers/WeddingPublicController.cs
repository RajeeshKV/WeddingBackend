using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using sib_api_v3_sdk.Api;
using sib_api_v3_sdk.Client;
using sib_api_v3_sdk.Model;
using WeddingInvite.Application.DTOs;
using WeddingInvite.Application.Interfaces;
using WeddingInvite.Application.Options;
using WeddingInvite.Domain.Entities;
using WeddingInvite.Infrastructure.Persistence;
using WeddingInvite.Infrastructure.Services;
using SystemTask = System.Threading.Tasks.Task;

namespace WeddingInvite.Api.Controllers;

[ApiController]
[Route("api/v1/public/weddings")]
public sealed class WeddingPublicController(
    WeddingInviteDbContext dbContext,
    IMemoryCache memoryCache,
    ICacheService cacheService,
    IOptions<BrevoOptions> brevoOptions,
    ILogger<WeddingPublicController> logger,
    ICloudinaryService cloudinaryService) : ControllerBase
{
    private readonly BrevoOptions _brevo = brevoOptions.Value;

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
    public async Task<IActionResult> GetEvents(string slug, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 50;
        if (pageSize > 100) pageSize = 100;
        var wedding = await dbContext.Weddings.AsNoTracking().FirstOrDefaultAsync(w => w.Slug == slug && w.IsPublished, cancellationToken);
        if (wedding is null) return NotFound();
        var query = dbContext.WeddingEvents.Where(e => e.WeddingId == wedding.Id).OrderBy(e => e.DisplayOrder);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(e => new WeddingEventDto(e.Id, e.Name, e.Description, e.StartTimeUtc, e.EndTimeUtc, e.Venue, e.Address, e.DisplayOrder))
            .ToListAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return Ok(new PagedResult<WeddingEventDto>(items, page, pageSize, totalCount, totalPages));
    }

    [HttpGet("{slug}/gallery")]
    public async Task<IActionResult> GetGallery(string slug, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 50) pageSize = 50;
        var wedding = await dbContext.Weddings.AsNoTracking().FirstOrDefaultAsync(w => w.Slug == slug && w.IsPublished, cancellationToken);
        if (wedding is null) return NotFound();
        var query = dbContext.Photos.Where(p => p.WeddingId == wedding.Id && p.Status == PhotoStatus.Approved).OrderByDescending(p => p.UploadedAtUtc);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(p => new PhotoDto(p.Id, p.ImageUrl, p.ThumbnailUrl, p.UploadedByName, p.Caption, p.Status.ToString(), p.UploadedAtUtc))
            .ToListAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return Ok(new PagedResult<PhotoDto>(items, page, pageSize, totalCount, totalPages));
    }

    [HttpPost("{slug}/rsvp")]
    public async Task<IActionResult> SubmitRsvp(string slug, RsvpRequest request, CancellationToken cancellationToken)
    {
        var wedding = await dbContext.Weddings.Include(w => w.AdminUser).FirstOrDefaultAsync(w => w.Slug == slug && w.IsPublished && w.RsvpEnabled, cancellationToken);
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
        _ = NotifyAdminOfRsvpAsync(wedding, rsvp);
        return Ok(new RsvpDto(rsvp.Id, rsvp.GuestName, rsvp.Email, rsvp.Phone, rsvp.Status.ToString(), rsvp.GuestCount, rsvp.Message, rsvp.SubmittedAtUtc));
    }

    [HttpPost("{slug}/photos/upload")]
    public async Task<IActionResult> UploadPhotos(string slug, [FromForm] List<IFormFile> files, [FromForm] string? uploadedByName, CancellationToken cancellationToken)
    {
        var wedding = await dbContext.Weddings.FirstOrDefaultAsync(w => w.Slug == slug && w.IsPublished && w.GuestUploadsEnabled, cancellationToken);
        if (wedding is null) return NotFound();
        if (files == null || files.Count == 0) return BadRequest("No files uploaded");
        
        var results = new List<object>();
        foreach (var file in files)
        {
            var (url, publicId) = await cloudinaryService.UploadAsync(file, cancellationToken);
            var photo = new Photo
            {
                WeddingId = wedding.Id,
                CloudinaryPublicId = publicId,
                ImageUrl = url,
                ThumbnailUrl = url,
                UploadedByName = uploadedByName,
                Status = wedding.AutoApprovePhotos ? PhotoStatus.Approved : PhotoStatus.Pending
            };
            dbContext.Photos.Add(photo);
            results.Add(new { id = photo.Id, status = photo.Status.ToString(), imageUrl = url });
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        cacheService.RemoveWedding(wedding.Id.ToString());
        return Ok(results);
    }

    private async SystemTask NotifyAdminOfRsvpAsync(Wedding wedding, Rsvp rsvp)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_brevo.ApiKey) || string.IsNullOrWhiteSpace(wedding.AdminUser?.Email)) return;
            Configuration.Default.AddApiKey("api-key", _brevo.ApiKey);
            var apiInstance = new TransactionalEmailsApi();
            var body = $"New RSVP for {wedding.Name}\n\nName: {rsvp.GuestName}\nEmail: {rsvp.Email}\nPhone: {rsvp.Phone}\nStatus: {rsvp.Status}\nGuests: {rsvp.GuestCount}\nMessage: {rsvp.Message}\n\nSubmitted: {rsvp.SubmittedAtUtc:u}";
            var sendSmtpEmail = new SendSmtpEmail(
                sender: new SendSmtpEmailSender(_brevo.SenderName, _brevo.SenderEmail),
                to: new List<SendSmtpEmailTo> { new SendSmtpEmailTo(wedding.AdminUser.Email) },
                subject: $"New RSVP - {wedding.Name}",
                textContent: body);
            await apiInstance.SendTransacEmailAsync(sendSmtpEmail);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send RSVP notification email for wedding {WeddingId}", wedding.Id);
        }
    }
}
