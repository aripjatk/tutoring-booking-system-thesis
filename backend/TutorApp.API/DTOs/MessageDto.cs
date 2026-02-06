namespace TutorApp.API.DTOs
{
    public class MessageDto
    {
        public int MessageID { get; set; }
        public string SenderUsername { get; set; }
        public string RecipientUsername { get; set; }
        public string Topic { get; set; }
        public string Body { get; set; }
        public string? AttachmentFileName { get; set; }
        public DateTime SentOn { get; set; }
    }
}
