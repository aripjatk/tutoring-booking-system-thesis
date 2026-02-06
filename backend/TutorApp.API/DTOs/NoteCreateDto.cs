namespace TutorApp.API.DTOs
{
    public class NoteCreateDto
    {
        public string AccountUsername { get; set; }
        public DateTime Date { get; set; }
        public string Body { get; set; }
    }
}
