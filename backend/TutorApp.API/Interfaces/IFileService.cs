using Microsoft.AspNetCore.Http;

namespace TutorApp.API.Interfaces {
    public interface IFileService {
        Task<string> SaveFileAsync(IFormFile file);
        string GetFilePath(string fileName);
    }
}
