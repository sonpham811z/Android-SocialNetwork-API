using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using NAudio.Wave;
using Post.Application.Interfaces;

namespace Post.Infrastructure.Services
{
    public class Mediaservice : IMediaService
    {
        private readonly Cloudinary _cloudinary;
        private readonly IConfiguration _configuration;

        public Mediaservice(IConfiguration configuration)
        {
            _configuration = configuration;

            var cloudinaryUrl = configuration["Cloudinary:CloudinaryUrl"];
            if (string.IsNullOrEmpty(cloudinaryUrl))
            {
                var account = new Account(
                    configuration["Cloudinary:CloudName"],
                    configuration["Cloudinary:ApiKey"],
                    configuration["Cloudinary:ApiSecret"]
                );
                _cloudinary = new Cloudinary(account);
            }
            else
            {
                _cloudinary = new Cloudinary(cloudinaryUrl);
            }
        }

        public async Task<(string Url, string PublicId)> UploadImageAsync(IFormFile file, string folder = "posts")
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty");

            //validate file
            var allowedExtensions = new[] {".jpg",".jpeg", ".png", ".gif", ".webp" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if(!allowedExtensions.Contains(extension))
                throw new ArgumentException("Invalid file style");

            if (file.Length > 10 * 1024 * 1024)
                throw new ArgumentException("File size exceeds 10MB limit");

            using var stream = file.OpenReadStream();

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = folder,
                Transformation = new Transformation()
                    .Width(1200).Height(1200).Crop("limit")
                    .Quality("auto")
                    .FetchFormat("auto"),
                UniqueFilename = true,
                Overwrite = false
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if(uploadResult.Error != null)
                throw new Exception($"Cloudinary upload error: {uploadResult.Error.Message}");

            return (uploadResult.SecureUrl.ToString(), uploadResult.PublicId);

        }

        public async Task<(string Url, string PublicId, string Duration, List<double> Waveform)> UploadAudioAsync(
            IFormFile file, string folder = "posts/audio")
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty");

            // Validate file type
            var allowedExtensions = new[] { ".mp3", ".wav", ".m4a", ".ogg", ".webm" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            
            if (!allowedExtensions.Contains(extension))
                throw new ArgumentException("Invalid file type. Only audio files are allowed.");

            // Validate file size (max 50MB for audio)
            if (file.Length > 50 * 1024 * 1024)
                throw new ArgumentException("File size exceeds 50MB limit");

            // Extract duration and waveform before upload
            string duration;
            List<double> waveform;

            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                // Get duration and waveform
                (duration, waveform) = ExtractAudioMetadata(memoryStream);
                
                // Reset stream for upload
                memoryStream.Position = 0;

                // Upload to Cloudinary
                var uploadParams = new VideoUploadParams
                {
                    File = new FileDescription(file.FileName, memoryStream),
                    Folder = folder,
                    UniqueFilename = true,
                    Overwrite = false
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                if (uploadResult.Error != null)
                    throw new Exception($"Cloudinary upload error: {uploadResult.Error.Message}");

                return (uploadResult.SecureUrl.ToString(), uploadResult.PublicId, duration, waveform);
            }
        }

        public async Task<(string Url, string PublicId, string? ThumbnailUrl, string? ThumbnailPublicId)> UploadVideoAsync(
            IFormFile file, string folder = "stories/video")
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty");

            var allowedExtensions = new[] { ".mp4", ".mov", ".avi", ".webm", ".mkv" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
                throw new ArgumentException("Invalid file type. Only video files are allowed.");

            if (file.Length > 100 * 1024 * 1024)
                throw new ArgumentException("File size exceeds 100MB limit");

            using var stream = file.OpenReadStream();

            var uploadParams = new VideoUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = folder,
                UniqueFilename = true,
                Overwrite = false
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
                throw new Exception($"Cloudinary upload error: {uploadResult.Error.Message}");

            // Build thumbnail URL from the uploaded video using Cloudinary's URL transformation
            // Cloudinary automatically generates frame thumbnails for video assets
            var thumbnailUrl = _cloudinary.Api.UrlVideoUp
                .Transform(new Transformation().Width(400).Height(700).Crop("fill").FetchFormat("jpg"))
                .BuildUrl($"{uploadResult.PublicId}.jpg");

            return (uploadResult.SecureUrl.ToString(), uploadResult.PublicId, thumbnailUrl, null);
        }

        public async Task<bool> DeleteImageAsync(string publicId)
        {
            if (string.IsNullOrEmpty(publicId))
                return false;

            try
            {
                var deleteParams = new DeletionParams(publicId)
                {
                    ResourceType = ResourceType.Image
                };

                var result = await _cloudinary.DestroyAsync(deleteParams);
                return result.Result == "ok";
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteAudioAsync(string publicId)
        {
            if (string.IsNullOrEmpty(publicId))
                return false;

            try
            {
                var deleteParams = new DeletionParams(publicId)
                {
                    ResourceType = ResourceType.Video
                };

                var result = await _cloudinary.DestroyAsync(deleteParams);
                return result.Result == "ok";
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteVideoAsync(string publicId)
        {
            if (string.IsNullOrEmpty(publicId))
                return false;

            try
            {
                var deleteParams = new DeletionParams(publicId)
                {
                    ResourceType = ResourceType.Video
                };

                var result = await _cloudinary.DestroyAsync(deleteParams);
                return result.Result == "ok";
            }
            catch
            {
                return false;
            }
        }

        private (string Duration, List<double> Waveform) ExtractAudioMetadata(Stream audioStream)
        {
            try
            {
                using var reader = new Mp3FileReader(audioStream);
                
                // Get duration
                var totalSeconds = reader.TotalTime.TotalSeconds;
                var minutes = (int)totalSeconds / 60;
                var seconds = (int)totalSeconds % 60;
                var duration = $"{minutes}:{seconds:D2}";

                // Generate waveform (sample at regular intervals)
                var waveform = GenerateWaveform(reader, 20); // 20 sample points

                return (duration, waveform);
            }
            catch
            {
                // Fallback if cannot read audio
                return ("0:00", GenerateDefaultWaveform());
            }
        }

        private List<double> GenerateWaveform(WaveStream reader, int sampleCount)
        {
            var waveForm  = new List<double>();
            var bytesPerSample = reader.WaveFormat.BitsPerSample / 8 * reader.WaveFormat.Channels;

            var samplesPerPoint = reader.Length / bytesPerSample / sampleCount;

            var buffer = new byte[bytesPerSample];
            
            for (int i = 0; i < sampleCount; i++)
            {
                var position = i * samplesPerPoint * bytesPerSample;
                
                if (position < reader.Length)
                {
                    reader.Position = position;
                    var bytesRead = reader.Read(buffer, 0, bytesPerSample);
                    
                    if (bytesRead > 0)
                    {
                        // Convert bytes to amplitude (0.0 to 1.0)
                        var sample = Math.Abs(BitConverter.ToInt16(buffer, 0)) / 32768.0;
                        waveForm.Add(Math.Round(sample, 2));
                    }
                    else
                    {
                        waveForm.Add(0.1);
                    }
                }
                else
                {
                    waveForm.Add(0.1);
                }
            }

            // Normalize waveForm
            if (waveForm.Any())
            {
                var max = waveForm.Max();
                if (max > 0)
                {
                    waveForm = waveForm.Select(w => Math.Round(w / max, 2)).ToList();
                }
            }

            return waveForm;
        }

        private List<double> GenerateDefaultWaveform()
        {
            // Return a default waveform pattern
            return new List<double> 
            { 
                0.3, 0.5, 0.8, 0.4, 0.6, 0.9, 0.5, 0.3, 0.7, 0.4, 
                0.6, 0.8, 0.5, 0.9, 0.3, 0.6, 0.8, 0.4, 0.7, 0.2 
            };
        }

    }

    
}