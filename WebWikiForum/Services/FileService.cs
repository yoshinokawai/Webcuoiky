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
                    // Tùy chọn: Biến đổi để giữ kích thước nhất quán
                    Transformation = new Transformation().Quality(100).Effect("improve").Effect("sharpen:100").FetchFormat("auto").Dpr("auto")
                };

                uploadResult = await _cloudinary.UploadAsync(uploadParams);
            }

            if (uploadResult.Error != null)
            {
                throw new Exception($"Cloudinary Upload Error: {uploadResult.Error.Message}");
            }

            // Trả về URL bảo mật đầy đủ
            return uploadResult.SecureUrl?.ToString();
        }

        public void DeleteFile(string fileName, string folderName)
        {
            // Lưu ý: Xóa file trên Cloudinary yêu cầu PublicId, không phải filename/URL.
            // Với cách triển khai đơn giản, có thể bỏ qua hoặc phân tích public ID từ URL.
            // Vì đây là demo, bỏ qua việc xóa cho đơn giản.
            if (string.IsNullOrEmpty(fileName)) return;
        }
    }
}
