using System;
using System.Threading.Tasks;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using User.Application.Interfaces;

namespace User.Infrastructure.Services
{
    public class CloudinaryService : ICloudinaryService
    {
        private readonly Cloudinary _cloudinary;
        private readonly IConfiguration _configuration;

        public CloudinaryService(IConfiguration configuration)
        {
            _configuration = configuration;

            var account = new Account(
                _configuration["Cloudinary:CloudName"],
                _configuration["Cloudinary:ApiKey"],
                _configuration["Cloudinary:ApiSecret"]
            );

            _cloudinary = new Cloudinary(account);
        }

        public async Task<CloudinaryUploadResult> UploadImageAsync(IFormFile file, string folder)
        {
            if (file == null || file.Length == 0)
            {
                return new CloudinaryUploadResult
                {
                    Success = false,
                    Error = "File is empty"
                };
            }

            var allowTypes = new[] {"image/jpeg", "image/jpg", "image/png"};
            if(!Array.Exists(allowTypes, type => type == file.ContentType.ToLower()))
            {
                return new CloudinaryUploadResult
                {
                    Success = false,
                    Error = "Invalid file type. Only JPEG, PNG are allowed",
                };
            }

            //validate file size
            var maxFileSize = 10 * 1024 * 1024; // 10MB
            if (file.Length > maxFileSize)
            {
                return new CloudinaryUploadResult
                {
                    Success = false,
                    Error = "File size exceeds 10MB limit"
                };
            }

            try
            {
                using var stream = file.OpenReadStream();

                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = folder,
                    Transformation = new Transformation()
                        .Width(1000)
                        .Height(1000)
                        .Crop("limit")
                        .Quality("auto:good")
                        .FetchFormat("auto"),
                    UniqueFilename = true,
                    Overwrite = false
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                if (uploadResult.Error != null)
                {
                    return new CloudinaryUploadResult
                    {
                        Success = false,
                        Error = uploadResult.Error.Message
                    };
                }

                return new CloudinaryUploadResult
                {
                    Success = true,
                    Url = uploadResult.SecureUrl.ToString(),
                    PublicId = uploadResult.PublicId
                };
            }
            catch (Exception ex)
            {
                return new CloudinaryUploadResult
                {
                    Success = false,
                    Error = $"Upload failed: {ex.Message}"
                };
            }
        }

        public async Task<bool> DeleteImageAsync(string publicId)
        {
            if (string.IsNullOrEmpty(publicId))
            {
                return false;
            }

            try
            {
                var deleteParams = new DeletionParams(publicId);
                var result = await _cloudinary.DestroyAsync(deleteParams);
                
                return result.Result == "ok";
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    
}