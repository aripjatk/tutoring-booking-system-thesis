using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TutorApp.API.Models
{
    public class Message
    {
        [Key]
        public int MessageID { get; set; }
        public string SenderUsername { get; set; }
        public string RecipientUsername { get; set; }
        public string Topic { get; set; }
        [Column(TypeName = "nvarchar(max)")]
        public string Body { get; set; }
        public string? AttachmentFileName { get; set; }
        public DateTime SentOn { get; set; }

        [ForeignKey("SenderUsername")]
        public Account Sender { get; set; }
        [ForeignKey("RecipientUsername")]
        public Account Recipient { get; set; }
    }
}
