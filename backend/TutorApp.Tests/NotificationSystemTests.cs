using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;
using TutorApp.API.Controllers;
using TutorApp.API.Data;
using TutorApp.API.DTOs;
using TutorApp.API.Interfaces;
using TutorApp.API.Models;
using Xunit;

namespace TutorApp.Tests
{
    public class NotificationSystemTests
    {
        private TutorDbContext GetDatabaseContext()
        {
            var options = new DbContextOptionsBuilder<TutorDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            var context = new TutorDbContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        private T GetController<T>(TutorDbContext context, string username, IFileService fileService = null) where T : ControllerBase
        {
            ControllerBase controller = null;

            if (typeof(T) == typeof(SessionController))
                controller = new SessionController(context);
            else if (typeof(T) == typeof(HomeworkAssignmentController))
                controller = new HomeworkAssignmentController(context, fileService ?? new Mock<IFileService>().Object);
            else if (typeof(T) == typeof(MessageController))
                controller = new MessageController(context, fileService ?? new Mock<IFileService>().Object);
            else if (typeof(T) == typeof(NotificationController))
                controller = new NotificationController(context);

            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, username),
            }, "mock"));

            controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = user }
            };

            return (T)controller;
        }

        private async Task SeedData(TutorDbContext context)
        {
            context.Account.Add(new Account { Username = "tutor1", IsTutor = true, DisplayName = "Tutor One", Email = "tutor1@example.com" });
            context.Account.Add(new Account { Username = "student1", IsTutor = false, DisplayName = "Student One", Email = "student1@example.com" });

            context.Course.Add(new Course { CourseID = 1, TutorUsername = "tutor1", Name = "Math", Description = "Math Course" });

            context.Session.Add(new Session { SessionID = 1, CourseID = 1, StudentUsername = "student1", ConfirmationStatus = ConfirmationStatus.Unknown });

            await context.SaveChangesAsync();
        }

        // Checks that a Tutor gets a notification if a student accepts a session created by them.
        [Fact]
        public async Task AcceptSession_ShouldNotifyTutor()
        {
            using var context = GetDatabaseContext();
            await SeedData(context);
            var controller = GetController<SessionController>(context, "student1");

            await controller.AcceptSession(1);

            var notification = await context.Notification.FirstOrDefaultAsync();
            Assert.NotNull(notification);
            Assert.Equal("tutor1", notification.AccountUsername);
            Assert.Equal(NotificationType.SessionAccepted, notification.NotificationType);
            Assert.Contains("accepted session", notification.Message);
        }

        // Checks that a Student gets a notification if they get a new homework assignment.
        [Fact]
        public async Task PostHomework_ShouldNotifyStudent()
        {
            using var context = GetDatabaseContext();
            await SeedData(context);
            var controller = GetController<HomeworkAssignmentController>(context, "tutor1");

            var dto = new HomeworkAssignmentCreateDto
            {
                SessionID = 1,
                Name = "Task 1",
                Objective = "Do the task 1 from page 67 of the exercise book",
                SolutionFeedback = ""
            };

            await controller.PostHomeworkAssignment(dto);

            var notification = await context.Notification.FirstOrDefaultAsync();
            Assert.NotNull(notification);
            Assert.Equal("student1", notification.AccountUsername);
            Assert.Equal(NotificationType.HomeworkAssigned, notification.NotificationType);
        }

        // Checks that if a user sends a message, the recipient of the message gets a notification.
        [Fact]
        public async Task PostMessage_ShouldNotifyRecipient()
        {
            using var context = GetDatabaseContext();
            await SeedData(context);
            var controller = GetController<MessageController>(context, "tutor1");

            var dto = new MessageCreateDto
            {
                RecipientUsername = "student1",
                Topic = "Hi",
                Body = "Hello"
            };

            await controller.PostMessage(dto);

            var notification = await context.Notification.FirstOrDefaultAsync();
            Assert.NotNull(notification);
            Assert.Equal("student1", notification.AccountUsername);
            Assert.Equal(NotificationType.MessageReceived, notification.NotificationType);
        }

        // Checks that a user can only see notifications intended for them.
        [Fact]
        public async Task GetNotifications_ShouldReturnOnlyOwn()
        {
            using var context = GetDatabaseContext();
            await SeedData(context);

            context.Notification.Add(new Notification { AccountUsername = "tutor1", Message = "For Tutor", NotificationType = NotificationType.MessageReceived });
            context.Notification.Add(new Notification { AccountUsername = "student1", Message = "For Student", NotificationType = NotificationType.MessageReceived });
            await context.SaveChangesAsync();

            var controller = GetController<NotificationController>(context, "tutor1");
            var result = await controller.GetNotifications();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var list = Assert.IsAssignableFrom<IEnumerable<NotificationDto>>(okResult.Value);
            Assert.Single(list);
            Assert.Equal("For Tutor", list.First().Message);
        }

        // Checks that a user cannot delete a notification that belongs to someone else.
        [Fact]
        public async Task DeleteNotification_User_CannotDeleteOthers() {
            using var context = GetDatabaseContext();
            await SeedData(context);

            var notif = new Notification { AccountUsername = "student1", Message = "Test", NotificationType = NotificationType.MessageReceived };
            await context.Notification.AddAsync(notif);
            await context.SaveChangesAsync();

            var controller = GetController<NotificationController>(context, "tutor1");
            var result = await controller.DeleteNotification(notif.NotificationID);

            Assert.IsType<ForbidResult>(result);
        }

        // Checks that a user can delete a notification that belongs to them.
        [Fact]
        public async Task DeleteNotification_User_CanDeleteOwn() {
            using var context = GetDatabaseContext();
            await SeedData(context);

            var notif = new Notification { AccountUsername = "student1", Message = "Test", NotificationType = NotificationType.MessageReceived };
            await context.Notification.AddAsync(notif);
            await context.SaveChangesAsync();

            var studentController = GetController<NotificationController>(context, "student1");
            var result2 = await studentController.DeleteNotification(notif.NotificationID);

            Assert.IsType<NoContentResult>(result2);
            Assert.Empty(context.Notification);
        }
    }
}
