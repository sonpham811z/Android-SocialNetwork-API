using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Message.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Message.Infrastructure.Services;

/// <summary>
/// Cloudinary-backed implementation of <see cref="IMediaService"/> for chat image attachments.
/// Mirrors the upload conventions used by the Post and User services.
/// </summary>
public class MediaService : IMediaService
{
    private readonly Cloudinary _cloudinary;

    private static readonly string[] AllowedExtensions =
        { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

    private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB

    public MediaService(IConfiguration configuration)
    {
        var cloudinaryUrl = configuration["Cloudinary:CloudinaryUrl"];
        if (!string.IsNullOrEmpty(cloudinaryUrl))
        {
            _cloudinary = new Cloudinary(cloudinaryUrl);
        }
        else
        {
            var account = new Account(
                configuration["Cloudinary:CloudName"],
                configuration["Cloudinary:ApiKey"],
                configuration["Cloudinary:ApiSecret"]);
            _cloudinary = new Cloudinary(account);
        }
    }

    public async Task<MediaUploadResult> UploadImageAsync(Stream content, string fileName, long length)
    {
        if (content is null || length == 0)
            throw new ArgumentException("File is empty.");

        if (length > MaxFileSize)
            throw new ArgumentException("File size exceeds the 10MB limit.");

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
            throw new ArgumentException("Invalid file type. Only image files are allowed.");

        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(fileName, content),
            Folder = "messages",
            Transformation = new Transformation()
                .Width(1600).Height(1600).Crop("limit")
                .Quality("auto")
                .FetchFormat("auto"),
            UniqueFilename = true,
            Overwrite = false
        };

        var result = await _cloudinary.UploadAsync(uploadParams);

        if (result.Error != null)
            throw new Exception($"Cloudinary upload error: {result.Error.Message}");

        return new MediaUploadResult(result.SecureUrl.ToString(), result.PublicId);
    }
}
