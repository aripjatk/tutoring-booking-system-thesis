using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TutorApp.API.Controllers;
using TutorApp.API.Data;
using TutorApp.API.DTOs;
using TutorApp.API.Models;
using Xunit;

namespace TutorApp.Tests
{
    public class SessionControllerTests
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

        private SessionController GetController(TutorDbContext context, string username)
        {
            var controller = new SessionController(context);

            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, username),
            }, "mock"));

            controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = user }
            };

            return controller;
        }

        private async Task SeedData(TutorDbContext context)
        {
            context.Account.Add(new Account { Username = "tutor1", IsTutor = true, DisplayName = "Tutor One", Email = "tutor1@example.com" });
            context.Account.Add(new Account { Username = "student1", IsTutor = false, DisplayName = "Student One", Email = "student1@example.com" });
            context.Account.Add(new Account { Username = "student2", IsTutor = false, DisplayName = "Student Two", Email = "student2@example.com" });

            context.Course.Add(new Course
            {
                CourseID = 1,
                Name = "Math 101",
                Description = "Math Course",
                TutorUsername = "tutor1",
                PricePerSession = 50
            });

            context.Session.Add(new Session
            {
                SessionID = 1,
                StudentUsername = "student1",
                CourseID = 1,
                SessionDateTime = DateTime.Now.AddDays(1)
            });

            context.Session.Add(new Session
            {
                SessionID = 2,
                StudentUsername = "student2",
                CourseID = 1,
                SessionDateTime = DateTime.Now.AddDays(2)
            });

            await context.SaveChangesAsync();
        }

        // Checks that a Tutor can retrieve details about a session
        // which is part of a course taught by them.
        [Fact]
        public async Task GetSession_Tutor_CanGetSessionForOwnCourse()
        {
            using var context = GetDatabaseContext();
            await SeedData(context);
            var controller = GetController(context, "tutor1");

            var result = await controller.GetSession(1);

            Assert.Equal(1, result.Value.SessionID);
            Assert.IsType<SessionDto>(result.Value);
        }

        // Checks that a Student can retrieve details about one of their sessions.
        [Fact]
        public async Task GetSession_Student_CanGetOwnSession()
        {
            using var context = GetDatabaseContext();
            await SeedData(context);
            var controller = GetController(context, "student1");

            var result = await controller.GetSession(1);

            Assert.Equal(1, result.Value.SessionID);
            Assert.IsType<SessionDto>(result.Value);
        }

        // Checks that a Student cannot retrieve details about another student's session.
        [Fact]
        public async Task GetSession_Student_CannotGetOtherSession()
        {
            using var context = GetDatabaseContext();
            await SeedData(context);
            var controller = GetController(context, "student1");

            var result = await controller.GetSession(2);

            Assert.IsType<ForbidResult>(result.Result);
        }

        // Checks that a Tutor can create a session with a time set in the future.
        [Fact]
        public async Task PostSession_Tutor_CanCreateFutureSession()
        {
            using var context = GetDatabaseContext();
            await SeedData(context);
            var controller = GetController(context, "tutor1");

            var newSession = new SessionCreateDto
            {
                StudentUsername = "student1",
                CourseID = 1,
                SessionDateTime = DateTime.UtcNow.AddDays(5)
            };

            var result = await controller.PostSession(newSession);
            var createdAtAction = Assert.IsType<CreatedAtActionResult>(result.Result);
            var createdSession = Assert.IsType<SessionDto>(createdAtAction.Value);

            Assert.Equal("student1", createdSession.StudentUsername);
        }

        // Checks that a Tutor cannot create a session with a time set in the past.
        [Fact]
        public async Task PostSession_Tutor_CannotCreatePastSession()
        {
            using var context = GetDatabaseContext();
            await SeedData(context);
            var controller = GetController(context, "tutor1");

            var newSession = new SessionCreateDto
            {
                StudentUsername = "student1",
                CourseID = 1,
                SessionDateTime = DateTime.UtcNow.AddDays(-1)
            };

            var result = await controller.PostSession(newSession);

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        // Checks that a Student cannot delete a session.
        [Fact]
        public async Task DeleteSession_Student_CannotDelete()
        {
            using var context = GetDatabaseContext();
            await SeedData(context);
            var controller = GetController(context, "student1");

            var result = await controller.DeleteSession(1);

            Assert.IsType<ForbidResult>(result);
        }
    }
}
