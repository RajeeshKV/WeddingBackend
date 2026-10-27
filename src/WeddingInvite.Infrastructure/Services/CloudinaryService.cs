using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using WeddingInvite.Application.Options;

namespace WeddingInvite.Infrastructure.Services;

public interface ICloudinaryService
{
    Task<(string Url, string PublicId)> UploadAsync(IFormFile file, CancellationToken cancellationToken);
    Task DeleteAsync(string publicId, CancellationToken cancellationToken);
}

public sealed class CloudinaryService : ICloudinaryService
{
    private readonly Cloudinary _cloudinary;

    public CloudinaryService(IOptions<CloudinaryOptions> options)
    {
        var opts = options.Value;
        var account = new Account(opts.CloudName, opts.ApiKey, opts.ApiSecret);
        _cloudinary = new Cloudinary(account);
    }

    public async Task<(string Url, string PublicId)> UploadAsync(IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            Folder = "wedding-invites"
        };
        var result = await _cloudinary.UploadAsync(uploadParams);
        return (result.SecureUrl.ToString(), result.PublicId);
    }

    public async Task DeleteAsync(string publicId, CancellationToken cancellationToken)
    {
        var deletionParams = new DeletionParams(publicId);
        await _cloudinary.DestroyAsync(deletionParams);
    }
}
