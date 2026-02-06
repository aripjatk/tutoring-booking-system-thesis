using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TutorApp.API.Models
{
    public class Session
    {
        [Key]
        public int SessionID { get; set; }
        public string StudentUsername { get; set; }
        public int CourseID { get; set; }
        public DateTime SessionDateTime { get; set; }
        public bool IsPaidFor { get; set; }
        public ConfirmationStatus ConfirmationStatus { get; set; }

        [ForeignKey("StudentUsername")]
        public Account Student { get; set; }
        [ForeignKey("CourseID")]
        public Course Course { get; set; }
        
        public ICollection<HomeworkAssignment> HomeworkAssignments { get; set; } = new List<HomeworkAssignment>();
    }
}
