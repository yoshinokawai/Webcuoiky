using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace WebWikiForum.Services
{
    public interface IFileService
    {
        Task<string> UploadImageAsync(IFormFile file, string folderName);
        void DeleteFile(string fileName, string folderName);
    }
}
