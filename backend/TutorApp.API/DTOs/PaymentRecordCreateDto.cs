using TutorApp.API.Models;

namespace TutorApp.API.DTOs
{
    public class PaymentRecordCreateDto
    {
        public string StudentUsername { get; set; }
        public string TutorUsername { get; set; }
        public decimal AmountPaid { get; set; }
        public MeansOfPayment MeansOfPayment { get; set; }
        public DateTime PaidOn { get; set; }
    }
}
