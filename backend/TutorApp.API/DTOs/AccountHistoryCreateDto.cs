using TutorApp.API.Models;

namespace TutorApp.API.DTOs
{
    public class AccountHistoryCreateDto
    {
        public string AccountUsername { get; set; }
        public EventType EventType { get; set; }
        public DateTime EventTimestamp { get; set; }
    }
}
