namespace TutorApp.API.DTOs
{
    public class SessionCreateDto
    {
        public string StudentUsername { get; set; }
        public int CourseID { get; set; }
        public DateTime SessionDateTime { get; set; }
        public bool IsPaidFor { get; set; }
    }
}
