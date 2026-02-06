using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;
using TutorApp.API.Interfaces;

namespace TutorApp.API.Services {
    public class FileService : IFileService
    {
        private readonly string _uploadsFolder = "Uploads";

        public FileService()
        {
        }

        public async Task<string> SaveFileAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return null;
            }

            string uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), _uploadsFolder);

            if (!Directory.Exists(uploadsPath))
            {
                Directory.CreateDirectory(uploadsPath);
            }

            string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
            string filePath = Path.Combine(uploadsPath, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return uniqueFileName;
        }

        public string GetFilePath(string fileName)
        {
            string uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), _uploadsFolder);
            return Path.Combine(uploadsPath, fileName);
        }
    }
}
