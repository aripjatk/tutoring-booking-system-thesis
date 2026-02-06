using Microsoft.EntityFrameworkCore;
using TutorApp.API.Data;
using TutorApp.API.Models;

namespace TutorApp.API.Services
{
    public class AccountCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AccountCleanupService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromHours(24);
        private readonly TimeSpan _cutoffPeriod = TimeSpan.FromDays(14);

        public AccountCleanupService(IServiceProvider serviceProvider, ILogger<AccountCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Account Cleanup Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await PerformCleanupAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred during account cleanup.");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }
        }

        private async Task PerformCleanupAsync(CancellationToken stoppingToken)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<TutorDbContext>();

                var cutoffDate = DateTime.Now.Subtract(_cutoffPeriod);

                var accountsToDelete = await context.Account
                    .Where(a => a.AccountHistory.Any() &&
                                a.AccountHistory
                                    .OrderByDescending(h => h.EventTimestamp)
                                    .Select(h => h.EventType)
                                    .FirstOrDefault() == EventType.Deactivation
                                &&
                                a.AccountHistory
                                    .OrderByDescending(h => h.EventTimestamp)
                                    .Select(h => h.EventTimestamp)
                                    .FirstOrDefault() < cutoffDate)
                    .ToListAsync(stoppingToken);

                if (accountsToDelete.Any())
                {
                    _logger.LogInformation($"Found {accountsToDelete.Count} accounts to delete.");

                    foreach (var account in accountsToDelete)
                    {
                        await DeleteAccountDataAsync(context, account);
                    }
                }
                else
                {
                    _logger.LogInformation("No accounts found for deletion.");
                }
            }
        }

        private async Task DeleteAccountDataAsync(TutorDbContext context, Account account)
        {
            try
            {
                var username = account.Username;

                var payments = await context.PaymentRecord
                    .Where(p => p.StudentUsername == username || p.TutorUsername == username)
                    .ToListAsync();
                if (payments.Any())
                    context.PaymentRecord.RemoveRange(payments);

                var messages = await context.Message
                    .Where(m => m.SenderUsername == username || m.RecipientUsername == username)
                    .ToListAsync();
                if (messages.Any())
                    context.Message.RemoveRange(messages);

                var sessions = await context.Session
                    .Where(s => s.StudentUsername == username)
                    .ToListAsync();
                if (sessions.Any())
                    context.Session.RemoveRange(sessions);

                var studentCourses = await context.StudentCourse
                    .Where(sc => sc.StudentUsername == username)
                    .ToListAsync();
                if (studentCourses.Any())
                    context.StudentCourse.RemoveRange(studentCourses);

                if (account.IsTutor)
                {
                    var courses = await context.Course
                        .Where(c => c.TutorUsername == username)
                        .ToListAsync();

                    foreach (var course in courses)
                    {
                        var enrolments = await context.StudentCourse
                             .Where(sc => sc.CourseID == course.CourseID)
                             .ToListAsync();
                        if (enrolments.Any())
                            context.StudentCourse.RemoveRange(enrolments);
                    }
                    if (courses.Any())
                        context.Course.RemoveRange(courses);
                }

                context.Account.Remove(account);

                await context.SaveChangesAsync();
                _logger.LogInformation($"Deleted account: {username}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to delete account {account.Username}");
            }
        }
    }
}
