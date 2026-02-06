namespace TutorApp.API.DTOs
{
    public class CourseDto
    {
        public int CourseID { get; set; }
        public string TutorUsername { get; set; }
        public string Name { get; set; }
        public decimal PricePerSession { get; set; }
        public string Description { get; set; }
    }
}
