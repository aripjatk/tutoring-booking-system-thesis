using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TutorApp.API.Models
{
    public class PaymentRecord
    {
        [Key]
        public int PaymentRecordID { get; set; }
        public string StudentUsername { get; set; }
        public string TutorUsername { get; set; }
        [Column(TypeName = "money")]
        public decimal AmountPaid { get; set; }
        public MeansOfPayment MeansOfPayment { get; set; }
        public DateTime PaidOn { get; set; }

        [ForeignKey("StudentUsername")]
        public Account Student { get; set; }
        [ForeignKey("TutorUsername")]
        public Account Tutor { get; set; }
    }
}
