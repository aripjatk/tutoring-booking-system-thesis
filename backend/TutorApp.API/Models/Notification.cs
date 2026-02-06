using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TutorApp.API.Models
{
    public class Notification
    {
        [Key]
        public int NotificationID { get; set; }
        public string AccountUsername { get; set; }
        public NotificationType NotificationType { get; set; }
        public string Message { get; set; }
        public DateTime NotificationTime { get; set; }

        [ForeignKey("AccountUsername")]
        public Account Account { get; set; }
    }
}
