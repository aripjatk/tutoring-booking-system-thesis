using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TutorApp.API.Models
{
    public class AccountHistory
    {
        [Key]
        public int HistoryEventID { get; set; }
        public string AccountUsername { get; set; }
        public EventType EventType { get; set; }
        public DateTime EventTimestamp { get; set; }

        [ForeignKey("AccountUsername")]
        public Account Account { get; set; }
    }
}
