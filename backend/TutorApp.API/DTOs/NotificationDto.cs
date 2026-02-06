using TutorApp.API.Models;

namespace TutorApp.API.DTOs
{
    public class NotificationDto
    {
        public int NotificationID { get; set; }
        public string AccountUsername { get; set; }
        public NotificationType NotificationType { get; set; }
        public string Message { get; set; }
        public DateTime NotificationTime { get; set; }
    }
}
