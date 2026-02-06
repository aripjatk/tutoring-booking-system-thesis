namespace TutorApp.API.DTOs
{
    public class HomeworkAssignmentDto
    {
        public int HomeworkAssignmentID { get; set; }
        public int SessionID { get; set; }
        public string Name { get; set; }
        public string Objective { get; set; }
        public bool HasSolutionFile { get; set; }
        public string? SolutionFeedback { get; set; }
    }
}
