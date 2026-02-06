using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TutorApp.API.Models
{
    public class Note
    {
        [Key]
        public int NoteID { get; set; }
        public string AccountUsername { get; set; }
        public DateTime Date { get; set; }
        [Column(TypeName = "nvarchar(max)")]
        public string Body { get; set; }

        [ForeignKey("AccountUsername")]
        public Account Account { get; set; }
    }
}
