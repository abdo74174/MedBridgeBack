using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MedBridge.Services
{
    public class CloudinaryService : ICloudinaryService
    {
        private readonly Cloudinary _cloudinary;
        private readonly ILogger<CloudinaryService> _logger;
        private readonly double _maxAllowedImageSize;


        public CloudinaryService(Cloudinary cloudinary, IConfiguration configuration, ILogger<CloudinaryService> logger)
        {
            _cloudinary = cloudinary ?? throw new ArgumentNullException(nameof(cloudinary));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _maxAllowedImageSize = configuration.GetValue<double>("ImageSettings:MaxAllowedImageSize", 10 * 1024 * 1024);
        }

        public async Task<string> UploadImageAsync(IFormFile file, string folder)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    _logger.LogWarning("No file provided for upload.");
                    throw new ArgumentException("No file provided.");
                }

                if (file.Length > _maxAllowedImageSize)
                {
                    _logger.LogWarning("Image size {Size} exceeds maximum allowed size {MaxSize}", file.Length, _maxAllowedImageSize);
                    throw new ArgumentException("Image size exceeds 10 MB.");
                }

                var ext = Path.GetExtension(file.FileName).ToLower();
                var allowedExtensions = new List<string> { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tiff", ".tif", ".svg" };
                if (!allowedExtensions.Contains(ext))
                {
                    _logger.LogWarning("Unsupported image format: {Extension}", ext);
                    throw new ArgumentException("Unsupported image format.");
                }

                using var stream = file.OpenReadStream();
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = folder
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                return uploadResult.SecureUrl.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading image to Cloudinary.");
                throw;
            }
        }
    }
}