using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TutorApp.API.Models
{
    public class Course
    {
        [Key]
        public int CourseID { get; set; }
        public string TutorUsername { get; set; }
        public string Name { get; set; }
        [Column(TypeName = "money")]
        public decimal PricePerSession { get; set; }
        public string Description { get; set; }

        [ForeignKey("TutorUsername")]
        public Account Tutor { get; set; }

        public ICollection<Student_Course> EnrolledStudents { get; set; } = new List<Student_Course>();
        public ICollection<TeachingMaterial> TeachingMaterials { get; set; } = new List<TeachingMaterial>();
        public ICollection<Session> Sessions { get; set; } = new List<Session>();
    }
}
