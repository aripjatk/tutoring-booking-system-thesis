using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TutorApp.API.Models
{
    public class HomeworkAssignment
    {
        [Key]
        public int HomeworkAssignmentID { get; set; }
        public int SessionID { get; set; }
        public string Name { get; set; }
        [Column(TypeName = "nvarchar(max)")]
        public string Objective { get; set; }
        public string? SolutionFileName { get; set; }
        [Column(TypeName = "nvarchar(max)")]
        public string? SolutionFeedback { get; set; }

        [ForeignKey("SessionID")]
        public Session Session { get; set; }
    }
}
