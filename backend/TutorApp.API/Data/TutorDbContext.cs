using Microsoft.EntityFrameworkCore;
using TutorApp.API.Models;

namespace TutorApp.API.Data
{
    public class TutorDbContext : DbContext
    {
        public TutorDbContext(DbContextOptions<TutorDbContext> options) : base(options)
        {
        }

        public DbSet<Account> Account { get; set; }
        public DbSet<AccountSettings> AccountSettings { get; set; }
        public DbSet<AccountHistory> AccountHistory { get; set; }
        public DbSet<Notification> Notification { get; set; }
        public DbSet<Note> Note { get; set; }
        public DbSet<Message> Message { get; set; }
        public DbSet<Course> Course { get; set; }
        public DbSet<Student_Course> StudentCourse { get; set; }
        public DbSet<Session> Session { get; set; }
        public DbSet<PaymentRecord> PaymentRecord { get; set; }
        public DbSet<TeachingMaterial> TeachingMaterial { get; set; }
        public DbSet<HomeworkAssignment> HomeworkAssignment { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Student_Course>()
                .HasKey(sc => new { sc.StudentUsername, sc.CourseID });

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany(a => a.SentMessages)
                .HasForeignKey(m => m.SenderUsername)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Recipient)
                .WithMany(a => a.ReceivedMessages)
                .HasForeignKey(m => m.RecipientUsername)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PaymentRecord>()
                .HasOne(pr => pr.Student)
                .WithMany(a => a.PaymentsMade)
                .HasForeignKey(pr => pr.StudentUsername)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PaymentRecord>()
                .HasOne(pr => pr.Tutor)
                .WithMany(a => a.PaymentsReceived)
                .HasForeignKey(pr => pr.TutorUsername)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Session>()
                .HasOne(s => s.Student)
                .WithMany(a => a.Sessions)
                .HasForeignKey(s => s.StudentUsername)
                .OnDelete(DeleteBehavior.Restrict); 

            modelBuilder.Entity<Student_Course>()
                .HasOne(sc => sc.Student)
                .WithMany(a => a.EnrolledCourses)
                .HasForeignKey(sc => sc.StudentUsername)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Student_Course>()
                .HasOne(sc => sc.Course)
                .WithMany(c => c.EnrolledStudents)
                .HasForeignKey(sc => sc.CourseID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Account>()
                .HasOne(a => a.AccountSettings)
                .WithOne(s => s.Account)
                .HasForeignKey<AccountSettings>(s => s.AccountUsername);
                
            modelBuilder.Entity<Course>()
                .HasOne(c => c.Tutor)
                .WithMany(a => a.TutoredCourses)
                .HasForeignKey(c => c.TutorUsername)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
