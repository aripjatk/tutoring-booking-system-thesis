using Microsoft.AspNetCore.Http;

namespace TutorApp.API.DTOs
{
    public class MessageCreateDto
    {
        public string SenderUsername { get; set; }
        public string RecipientUsername { get; set; }
        public string Topic { get; set; }
        public string Body { get; set; }
        public IFormFile? File { get; set; }
    }
}
