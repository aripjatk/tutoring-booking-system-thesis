namespace TutorApp.API.DTOs
{
    public class CourseCreateDto
    {
        public string TutorUsername { get; set; }
        public string Name { get; set; }
        public decimal PricePerSession { get; set; }
        public string Description { get; set; }
    }
}
