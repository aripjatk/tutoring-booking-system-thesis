using Microsoft.AspNetCore.Http;

namespace TutorApp.API.DTOs
{
    public class TeachingMaterialCreateDto
    {
        public string Name { get; set; }
        public int CourseID { get; set; }
        public IFormFile File { get; set; }
    }
}
