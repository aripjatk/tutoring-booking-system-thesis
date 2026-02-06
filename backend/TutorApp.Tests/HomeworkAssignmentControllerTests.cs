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
    public class HomeworkAssignmentControllerTests
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

        private HomeworkAssignmentController GetController(TutorDbContext context, string username, IFileService fileService = null)
        {
            if (fileService == null)
            {
                var mockFileService = new Mock<IFileService>();
                fileService = mockFileService.Object;
            }

            var controller = new HomeworkAssignmentController(context, fileService);

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
            context.Account.Add(new Account { Username = "tutor2", IsTutor = true, DisplayName = "Tutor Two", Email = "tutor2@example.com" });

            context.Account.Add(new Account { Username = "student1", IsTutor = false, DisplayName = "Student One", Email = "student1@example.com" });
            context.Account.Add(new Account { Username = "student2", IsTutor = false, DisplayName = "Student Two", Email = "student2@example.com" });

            context.Course.Add(new Course { CourseID = 1, TutorUsername = "tutor1", Name = "Math", Description = "Math Course" });
            context.Course.Add(new Course { CourseID = 2, TutorUsername = "tutor2", Name = "English", Description = "English Course" });

            context.Session.Add(new Session { SessionID = 1, CourseID = 1, StudentUsername = "student1" });
            context.Session.Add(new Session { SessionID = 2, CourseID = 2, StudentUsername = "student2" });

            context.HomeworkAssignment.Add(new HomeworkAssignment { HomeworkAssignmentID = 1, SessionID = 1, Name = "Math HW 1", Objective = "Solve problems", SolutionFileName = "sol1.pdf", SolutionFeedback = "Good" });
            context.HomeworkAssignment.Add(new HomeworkAssignment { HomeworkAssignmentID = 2, SessionID = 2, Name = "English HW 1", Objective = "Write essay", SolutionFileName = "sol2.pdf", SolutionFeedback = "Bad" });

            await context.SaveChangesAsync();
        }

        // Checks that a Tutor only sees homework for their own courses.
        [Fact]
        public async Task GetHomeworkAssignments_Tutor_ShouldReturnOnlyAssignmentsForTheirCourses()
        {
            using var context = GetDatabaseContext();
            await SeedData(context);
            var controller = GetController(context, "tutor1");

            var result = await controller.GetHomeworkAssignments();

            var actionResult = Assert.IsType<ActionResult<IEnumerable<HomeworkAssignmentDto>>>(result);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var list = Assert.IsAssignableFrom<IEnumerable<HomeworkAssignmentDto>>(okResult.Value);

            // Should see Assignment 1 (Course 1, Tutor 1)
            // Should NOT see Assignment 2 (Course 2, Tutor 2)
            Assert.Contains(list, a => a.HomeworkAssignmentID == 1);
            Assert.DoesNotContain(list, a => a.HomeworkAssignmentID == 2);
        }

        // Checks that a Student only sees homework for their sessions.
        [Fact]
        public async Task GetHomeworkAssignments_Student_ShouldReturnOnlyAssignmentsForTheirSessions()
        {
            using var context = GetDatabaseContext();
            await SeedData(context);
            var controller = GetController(context, "student1");

            var result = await controller.GetHomeworkAssignments();

            var actionResult = Assert.IsType<ActionResult<IEnumerable<HomeworkAssignmentDto>>>(result);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var list = Assert.IsAssignableFrom<IEnumerable<HomeworkAssignmentDto>>(okResult.Value);

            // Should see Assignment 1 (Session 1, Student 1)
            // Should NOT see Assignment 2 (Session 2, Student 2)
            Assert.Contains(list, a => a.HomeworkAssignmentID == 1);
            Assert.DoesNotContain(list, a => a.HomeworkAssignmentID == 2);
        }

        // Checks that a Tutor can retrieve details about a homework assignment
        // for a session which is part of a course taught by them.
        [Fact]
        public async Task GetHomeworkAssignment_Tutor_CanAccessOwnCourseAssignment()
        {
            using var context = GetDatabaseContext();
            await SeedData(context);
            var controller = GetController(context, "tutor1");

            var result = await controller.GetHomeworkAssignment(1);

            Assert.IsType<HomeworkAssignmentDto>(result.Value);
            Assert.Equal(1, result.Value.HomeworkAssignmentID);
        }

        // Checks that a Tutor cannot retrieve details about a homework assignment
        // for a session which is not part of a course taught by them.
        [Fact]
        public async Task GetHomeworkAssignment_Tutor_CannotAccessOtherCourseAssignment()
        {
            using var context = GetDatabaseContext();
            await SeedData(context);
            var controller = GetController(context, "tutor1");

            var result = await controller.GetHomeworkAssignment(2);

            Assert.IsType<ForbidResult>(result.Result);
        }

        // Checks that a Student can access a homework assignment connected to their session.
        [Fact]
        public async Task GetHomeworkAssignment_Student_CanAccessOwnSessionAssignment()
        {
            using var context = GetDatabaseContext();
            await SeedData(context);
            var controller = GetController(context, "student1");

            var result = await controller.GetHomeworkAssignment(1);

            Assert.IsType<HomeworkAssignmentDto>(result.Value);
            Assert.Equal(1, result.Value.HomeworkAssignmentID);
        }

        // Checks that a Student cannot access a homework assignment which is not connected
        // to their session.
        [Fact]
        public async Task GetHomeworkAssignment_Student_CannotAccessOtherSessionAssignment()
        {
            using var context = GetDatabaseContext();
            await SeedData(context);
            var controller = GetController(context, "student1");

            var result = await controller.GetHomeworkAssignment(2);

            Assert.IsType<ForbidResult>(result.Result);
        }

        // Checks that a Tutor can create a homework assignment for a session
        // which is part of a course taught by them.
        [Fact]
        public async Task PostHomeworkAssignment_Tutor_CanCreateForOwnCourse()
        {
            using var context = GetDatabaseContext();
            await SeedData(context);
            var controller = GetController(context, "tutor1");

            var newAssignment = new HomeworkAssignmentCreateDto
            {
                SessionID = 1,
                Name = "New Math Homework",
                Objective = "Test",
                SolutionFeedback = "None",
                File = new FormFile(new MemoryStream(new byte[1]), 0, 1, "Data", "test.pdf")
            };

            var mockFileService = new Mock<IFileService>();
            mockFileService.Setup(f => f.SaveFileAsync(It.IsAny<IFormFile>())).ReturnsAsync("test.pdf");

            controller = GetController(context, "tutor1", mockFileService.Object);

            var result = await controller.PostHomeworkAssignment(newAssignment);

            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var createdAssignment = Assert.IsType<HomeworkAssignmentDto>(createdAtActionResult.Value);
            Assert.Equal("New Math Homework", createdAssignment.Name);
        }

        // A Tutor cannot create a homework assignment for a session
        // which is not part of a course taught by them.
        [Fact]
        public async Task PostHomeworkAssignment_Tutor_CannotCreateForOtherCourse()
        {
            using var context = GetDatabaseContext();
            await SeedData(context);
            var controller = GetController(context, "tutor1");

            var newAssignment = new HomeworkAssignmentCreateDto
            {
                SessionID = 2,
                Name = "New Math Homework",
                Objective = "Test",
                SolutionFeedback = "None",
                File = new FormFile(new MemoryStream(new byte[1]), 0, 1, "Data", "test.pdf")
            };

            var mockFileService = new Mock<IFileService>();
            mockFileService.Setup(f => f.SaveFileAsync(It.IsAny<IFormFile>())).ReturnsAsync("test.pdf");
            controller = GetController(context, "tutor1", mockFileService.Object);

            var result = await controller.PostHomeworkAssignment(newAssignment);

            Assert.IsType<ForbidResult>(result.Result);
        }

        // Check that a Student cannot create homework assignments.
        [Fact]
        public async Task PostHomeworkAssignment_Student_CannotCreate()
        {
            using var context = GetDatabaseContext();
            await SeedData(context);
            var controller = GetController(context, "student1");

            var newAssignment = new HomeworkAssignmentCreateDto
            {
                SessionID = 1,
                Name = "Student HW",
                Objective = "Test",
                SolutionFeedback = "None",
                File = new FormFile(new MemoryStream(new byte[1]), 0, 1, "Data", "test.pdf")
            };

            var mockFileService = new Mock<IFileService>();
            mockFileService.Setup(f => f.SaveFileAsync(It.IsAny<IFormFile>())).ReturnsAsync("test.pdf");
            controller = GetController(context, "student1", mockFileService.Object);

            var result = await controller.PostHomeworkAssignment(newAssignment);

            Assert.IsType<ForbidResult>(result.Result);
        }

        // Checks that a Tutor can update a homework assignment for a session
        // which is part of a course taught by them.
        [Fact]
        public async Task PutHomeworkAssignment_Tutor_CanUpdateOwnCourseAssignment()
        {
            using var context = GetDatabaseContext();
            await SeedData(context);
            var controller = GetController(context, "tutor1");

            var assignment = await context.HomeworkAssignment.FindAsync(1);
            assignment.SolutionFeedback = "Grade 5 - Very good. Good job!";

            var result = await controller.PutHomeworkAssignment(1, assignment);

            Assert.IsType<NoContentResult>(result);
        }

        // Checks that a Tutor cannot update a homework assignment for a session
        // which is not part of a course taught by them.
        [Fact]
        public async Task PutHomeworkAssignment_Tutor_CannotUpdateOtherCourseAssignment()
        {
            using var context = GetDatabaseContext();
            await SeedData(context);
            var controller = GetController(context, "tutor1");

            var assignment = await context.HomeworkAssignment.FindAsync(2);
            assignment.SolutionFeedback = "Grade 2 - Unsatisfactory. Do better next time!";

            var result = await controller.PutHomeworkAssignment(2, assignment);

            Assert.IsType<ForbidResult>(result);
        }

        // Checks that a Student cannot update a homework assignment.
        [Fact]
        public async Task PutHomeworkAssignment_Student_CannotUpdate()
        {
            using var context = GetDatabaseContext();
            await SeedData(context);
            var controller = GetController(context, "student1");

            var assignment = await context.HomeworkAssignment.FindAsync(1);
            assignment.SolutionFeedback = "grade 9999 the goodest!!!";

            var result = await controller.PutHomeworkAssignment(1, assignment);

            Assert.IsType<ForbidResult>(result);
        }

        // Checks that a Tutor can delete a homework assignment
        // for a session which is part of a course taught by them.
        [Fact]
        public async Task DeleteHomeworkAssignment_Tutor_CanDeleteOwnCourseAssignment()
        {
            using var context = GetDatabaseContext();
            await SeedData(context);
            var controller = GetController(context, "tutor1");

            var result = await controller.DeleteHomeworkAssignment(1);

            Assert.IsType<NoContentResult>(result);
        }

        // Checks that a Tutor cannot delete a homework assignment
        // for a session which is not part of a course taught by them.
        [Fact]
        public async Task DeleteHomeworkAssignment_Tutor_CannotDeleteOtherCourseAssignment()
        {
            using var context = GetDatabaseContext();
            await SeedData(context);
            var controller = GetController(context, "tutor1");

            var result = await controller.DeleteHomeworkAssignment(2);

            Assert.IsType<ForbidResult>(result);
        }

        // Checks that a Student cannot delete a homework assignment.
        [Fact]
        public async Task DeleteHomeworkAssignment_Student_CannotDelete()
        {
            using var context = GetDatabaseContext();
            await SeedData(context);
            var controller = GetController(context, "student1");

            var result = await controller.DeleteHomeworkAssignment(1);

            Assert.IsType<ForbidResult>(result);
        }
    }
}
