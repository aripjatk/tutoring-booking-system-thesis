namespace TutorApp.API.DTOs
{
    public class StudentCourseCreateDto
    {
        public string StudentUsername { get; set; }
        public int CourseID { get; set; }
        public string Frequency { get; set; }
        public DateTime EndDate { get; set; }
    }
}
