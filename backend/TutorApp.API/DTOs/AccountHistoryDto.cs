using TutorApp.API.Models;

namespace TutorApp.API.DTOs
{
    public class AccountHistoryDto
    {
        public int HistoryEventID { get; set; }
        public string AccountUsername { get; set; }
        public EventType EventType { get; set; }
        public DateTime EventTimestamp { get; set; }
    }
}
