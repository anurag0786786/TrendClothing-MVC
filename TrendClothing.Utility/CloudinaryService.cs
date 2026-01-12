using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace TrendClothing.Utility
{
    public class CloudinaryService
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryService(IConfiguration config)
        {

            var cloudName = config["Cloudinary:CloudName"];
            var apiKey = config["Cloudinary:ApiKey"];
            var apiSecret = config["Cloudinary:ApiSecret"];

            if (string.IsNullOrEmpty(cloudName))
                throw new Exception("Cloudinary CloudName missing – check appsettings");

            var account = new Account(cloudName, apiKey, apiSecret);
            _cloudinary = new Cloudinary(account);
            Console.WriteLine("Cloudinary CloudName = " + cloudName);

        }


        public async Task<string?> UploadImageAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return null;

            using var stream = file.OpenReadStream();

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = "trendclothing/products",
                UseFilename = true,
                UniqueFilename = true,
                Overwrite = false
            };

            var result = await _cloudinary.UploadAsync(uploadParams);

            // ✅ IMPORTANT SAFETY CHECK
            if (result == null || result.Error != null)
            {
                var errorMsg = result?.Error?.Message ?? "Unknown Cloudinary error";
                throw new Exception("Cloudinary Upload Failed: " + errorMsg);
            }

            return result.SecureUrl?.ToString();
        }

    }
}
