using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TutorApp.API.Models
{
    public class Student_Course
    {
        public string StudentUsername { get; set; }
        public int CourseID { get; set; }
        public string Frequency { get; set; }
        public DateTime EndDate { get; set; }

        [ForeignKey("StudentUsername")]
        public Account Student { get; set; }
        [ForeignKey("CourseID")]
        public Course Course { get; set; }
    }
}
