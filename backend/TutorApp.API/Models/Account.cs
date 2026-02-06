using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TutorApp.API.Models
{
    public class Account
    {
        [Key]
        public string Username { get; set; }
        public string DisplayName { get; set; }
        public string Email { get; set; }
        public bool IsActive { get; set; }
        public bool IsTutor { get; set; }

        public AccountSettings AccountSettings { get; set; }
        public ICollection<AccountHistory> AccountHistory { get; set; } = new List<AccountHistory>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        public ICollection<Note> Notes { get; set; } = new List<Note>();

        [InverseProperty("Sender")]
        public ICollection<Message> SentMessages { get; set; } = new List<Message>();
        [InverseProperty("Recipient")]
        public ICollection<Message> ReceivedMessages { get; set; } = new List<Message>();

        [InverseProperty("Tutor")]
        public ICollection<Course> TutoredCourses
            { get {
                if (IsTutor)
                    return TutoredCourses;
                else
                    throw new InvalidOperationException("Attempted to obtain TutoredCourses for a Student");
            } set; } = new List<Course>();

        public ICollection<Student_Course> EnrolledCourses { get {
                if (IsTutor)
                    throw new InvalidOperationException("Attempted to obtain EnrolledCourses for a Tutor");
                else
                    return EnrolledCourses;
            } set; } = new List<Student_Course>();

        [InverseProperty("Student")]
        public ICollection<Session> Sessions { get {
                if (IsTutor)
                    throw new InvalidOperationException("Attempted to obtain Session for a Tutor");
                else
                    return Sessions;
            } set; } = new List<Session>();

        [InverseProperty("Student")]
        public ICollection<PaymentRecord> PaymentsMade { get {
                if (IsTutor)
                    throw new InvalidOperationException("Attempted to obtain PaymentsMade for a Tutor");
                else
                    return PaymentsMade;
            } set; } = new List<PaymentRecord>();
        [InverseProperty("Tutor")]
        public ICollection<PaymentRecord> PaymentsReceived { get {
                if (IsTutor)
                    return PaymentsReceived;
                else
                    throw new InvalidOperationException("Attempted to obtain PaymentsReceived for a Student");
            } set; } = new List<PaymentRecord>();
    }
}
