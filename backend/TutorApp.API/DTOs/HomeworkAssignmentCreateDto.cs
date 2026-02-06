using Microsoft.AspNetCore.Http;

namespace TutorApp.API.DTOs
{
    public class HomeworkAssignmentCreateDto
    {
        public int SessionID { get; set; }
        public string Name { get; set; }
        public string Objective { get; set; }
    }
}
