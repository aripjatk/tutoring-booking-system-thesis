using TutorApp.API.Models;

namespace TutorApp.API.DTOs
{
    public class SessionDto
    {
        public int SessionID { get; set; }
        public string StudentUsername { get; set; }
        public int CourseID { get; set; }
        public DateTime SessionDateTime { get; set; }
        public bool IsPaidFor { get; set; }
        public ConfirmationStatus ConfirmationStatus { get; set; }
        public IEnumerable<HomeworkAssignmentDto>? HomeworkAssignments { get; set; }
    }
}
