using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TutorApp.API.Models
{
    public class TeachingMaterial
    {
        [Key]
        public int TeachingMaterialID { get; set; }
        public string Name { get; set; }
        public string FileName { get; set; }
        public int CourseID { get; set; }

        [ForeignKey("CourseID")]
        public Course Course { get; set; }
    }
}
