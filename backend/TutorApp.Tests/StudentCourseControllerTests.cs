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
    public class StudentCourseControllerTests
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

        private StudentCourseController GetController(TutorDbContext context, string username)
        {
            var controller = new StudentCourseController(context);

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

            context.Course.Add(new Course
            {
                CourseID = 1,
                Name = "Math 101",
                Description = "Math Course",
                TutorUsername = "tutor1",
                PricePerSession = 50
            });

            context.StudentCourse.Add(new Student_Course
            {
                StudentUsername = "student1",
                CourseID = 1,
                Frequency = "",
                EndDate = DateTime.Now.AddMonths(1)
            });

            await context.SaveChangesAsync();
        }

        // Checks that a Tutor can enrol a student with a future enrolment end date.
        [Fact]
        public async Task PostStudentCourse_Tutor_CanEnrolStudentWithFutureDate()
        {
            using var context = GetDatabaseContext();
            await SeedData(context);
            var controller = GetController(context, "tutor1");

            var newAssignment = new StudentCourseCreateDto
            {
                StudentUsername = "student2",
                CourseID = 1,
                Frequency = "",
                EndDate = DateTime.Now.AddMonths(2)
            };

            var result = await controller.PostStudentCourse(newAssignment);
            var createdAtAction = Assert.IsType<CreatedAtActionResult>(result.Result);
            var createdAssignment = Assert.IsType<StudentCourseDto>(createdAtAction.Value);

            Assert.Equal("student2", createdAssignment.StudentUsername);
        }

        // Checks that a Tutor cannot enrol a student with an enrolment date that is in the past.
        [Fact]
        public async Task PostStudentCourse_Tutor_CannotEnrolStudentWithPastDate()
        {
            using var context = GetDatabaseContext();
            await SeedData(context);
            var controller = GetController(context, "tutor1");

            var newAssignment = new StudentCourseCreateDto
            {
                StudentUsername = "student2",
                CourseID = 1,
                Frequency = "",
                EndDate = DateTime.Now.AddMonths(-1)
            };

            var result = await controller.PostStudentCourse(newAssignment);

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        // Checks that a Student can retrieve details about one of their own enrolments.
        [Fact]
        public async Task GetStudentCourse_Student_CanGetOwnEnrolment()
        {
            using var context = GetDatabaseContext();
            await SeedData(context);
            var controller = GetController(context, "student1");

            var result = await controller.GetStudentCourse("student1", 1);

            Assert.Equal("student1", result.Value.StudentUsername);
            Assert.IsType<StudentCourseDto>(result.Value);
        }

        // Checks that a Tutor can retrieve details about an enrolment for one of their own courses.
        [Fact]
        public async Task GetStudentCourse_Tutor_CanGetEnrolmentForOwnCourse()
        {
            using var context = GetDatabaseContext();
            await SeedData(context);
            var controller = GetController(context, "tutor1");

            var result = await controller.GetStudentCourse("student1", 1);

            Assert.Equal("student1", result.Value.StudentUsername);
            Assert.IsType<StudentCourseDto>(result.Value);
        }

        // Checks that a Student cannot retrieve details about a different student's enrolment.
        [Fact]
        public async Task GetStudentCourse_Student_CannotGetUnrelatedEnrolment()
        {
            using var context = GetDatabaseContext();
            await SeedData(context);
            var controller = GetController(context, "student2");

            var result = await controller.GetStudentCourse("student1", 1);

            Assert.IsType<ForbidResult>(result.Result);
        }

        // Checks that a Tutor cannot retrieve details about a different tutor's enrolment.
        [Fact]
        public async Task GetStudentCourse_Tutor_CannotGetUnrelatedEnrolment()
        {
            using var context = GetDatabaseContext();
            await SeedData(context);
            var controller = GetController(context, "tutor2");

            var result = await controller.GetStudentCourse("student1", 1);

            Assert.IsType<ForbidResult>(result.Result);
        }
    }
}
