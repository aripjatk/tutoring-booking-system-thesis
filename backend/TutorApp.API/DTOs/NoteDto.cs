namespace TutorApp.API.DTOs
{
    public class NoteDto
    {
        public int NoteID { get; set; }
        public string AccountUsername { get; set; }
        public DateTime Date { get; set; }
        public string Body { get; set; }
    }
}
