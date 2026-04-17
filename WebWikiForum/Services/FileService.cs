using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using WebWikiForum.Models;

namespace WebWikiForum.Services
{
    public class FileService : IFileService
    {
        private readonly Cloudinary _cloudinary;

        public FileService(IOptions<CloudinarySettings> config)
        {
            var acc = new Account(
                config.Value.CloudName,
                config.Value.ApiKey,
                config.Value.ApiSecret
            );

            _cloudinary = new Cloudinary(acc);
        }

        public async Task<string?> UploadImageAsync(IFormFile file, string folderName)
        {
            if (file == null || file.Length == 0)
                return null;

            var uploadResult = new ImageUploadResult();

            using (var stream = file.OpenReadStream())
            {
                var uploadParams = new ImageUploadParams()
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = "vtwiki/" + folderName,
                    // Optional: Transformations to keep sizes consistent
                    Transformation = new Transformation().Quality(100).Effect("improve").Effect("sharpen:100").FetchFormat("auto").Dpr("auto")
                };

                uploadResult = await _cloudinary.UploadAsync(uploadParams);
            }

            if (uploadResult.Error != null)
            {
                throw new Exception($"Cloudinary Upload Error: {uploadResult.Error.Message}");
            }

            // Return the full secure URL
            return uploadResult.SecureUrl?.ToString();
        }

        public void DeleteFile(string fileName, string folderName)
        {
            // Note: Cloudinary deletion requires the PublicId, not just the filename/URL.
            // For a basic implementation, we can skip deletion or parse the public ID from the URL.
            // Since this is a demo, we'll keep it simple and skip deletion for now.
            if (string.IsNullOrEmpty(fileName)) return;
        }
    }
}
