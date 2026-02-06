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
    public class CourseControllerTests
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

        private CourseController GetController(TutorDbContext context, string username)
        {
            var controller = new CourseController(context);

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
                Description = "Intro to Math",
                PricePerSession = 50,
                TutorUsername = "tutor1"
            });

            context.Course.Add(new Course
            {
                CourseID = 2,
                Name = "Physics 101",
                Description = "Intro to Physics",
                PricePerSession = 60,
                TutorUsername = "tutor2"
            });

            context.StudentCourse.Add(new Student_Course
            {
                StudentUsername = "student1",
                CourseID = 1,
                Frequency = "Weekly",
                EndDate = DateTime.Now.AddMonths(1)
            });

            await context.SaveChangesAsync();
        }

        // Checks that a tutor can retrieve information about a course taught by them.
        [Fact]
        public async Task GetCourse_Tutor_CanGetOwn()
        {
            using var context = GetDatabaseContext();
            await SeedData(context);
            var controller = GetController(context, "tutor1");

            var result = await controller.GetCourse(1);

            Assert.Equal(1, result.Value.CourseID);
            Assert.IsType<CourseDto>(result.Value);
        }

        // Checks that a Tutor cannot retrieve information about other tutors' courses.
        [Fact]
        public async Task GetCourse_Tutor_CannotGetOther()
        {
            using var context = GetDatabaseContext();
            await SeedData(context);
            var controller = GetController(context, "tutor1");

            var result = await controller.GetCourse(2);

            Assert.IsType<ForbidResult>(result.Result);
        }

        // Checks that a Student can retrieve information about a course in which they are
        // enrolled (Student_Course combination exists).
        [Fact]
        public async Task GetCourse_Student_CanGetEnrolled()
        {
            using var context = GetDatabaseContext();
            await SeedData(context);
            var controller = GetController(context, "student1");

            var result = await controller.GetCourse(1);

            Assert.Equal(1, result.Value.CourseID);
            Assert.IsType<CourseDto>(result.Value);
        }

        // Checks that a Student cannot retrieve information about a course in which
        // they are not enrolled.
        [Fact]
        public async Task GetCourse_Student_CannotGetNotEnrolled()
        {
            using var context = GetDatabaseContext();
            await SeedData(context);
            var controller = GetController(context, "student1");

            var result = await controller.GetCourse(2);

            Assert.IsType<ForbidResult>(result.Result);
        }

        // Checks that a Tutor can create a course.
        [Fact]
        public async Task PostCourse_Tutor_CanCreate()
        {
            using var context = GetDatabaseContext();
            await SeedData(context);
            var controller = GetController(context, "tutor1");

            var newCourse = new CourseCreateDto
            {
                Name = "Chemistry 101",
                Description = "Intro to Chemistry",
                PricePerSession = 55,
                TutorUsername = "tutor1"
            };

            var result = await controller.PostCourse(newCourse);
            var createdAtAction = Assert.IsType<CreatedAtActionResult>(result.Result);
            var createdCourse = Assert.IsType<CourseDto>(createdAtAction.Value);

            Assert.Equal("tutor1", createdCourse.TutorUsername);
        }

        // Checks that a Student cannot create a course.
        [Fact]
        public async Task PostCourse_Student_CannotCreate()
        {
            using var context = GetDatabaseContext();
            await SeedData(context);
            var controller = GetController(context, "student1");

            var newCourse = new CourseCreateDto {
                Name = "Chemistry 101",
                Description = "Intro to Chemistry",
                PricePerSession = 55,
                TutorUsername = "tutor1"
            };

            var result = await controller.PostCourse(newCourse);
            var forbidAction = Assert.IsType<ForbidResult>(result.Result);
        }

        // Checks that a Tutor can delete a course taught by them.
        [Fact]
        public async Task DeleteCourse_Tutor_CanDeleteOwn()
        {
             using var context = GetDatabaseContext();
             await SeedData(context);

             // Create a course without enrollments for deletion test
             context.Course.Add(new Course { CourseID = 3, Name = "History", Description = "History 101", PricePerSession = 40, TutorUsername = "tutor1" });
             await context.SaveChangesAsync();

             var controller = GetController(context, "tutor1");

             var result = await controller.DeleteCourse(3);

             Assert.IsType<NoContentResult>(result);
             Assert.Null(await context.Course.FindAsync(3));
        }

        // Checks that a Student cannot delete a course.
        [Fact]
        public async Task DeleteCourse_Student_CannotDelete()
        {
            using var context = GetDatabaseContext();
            await SeedData(context);
            var controller = GetController(context, "student1");

            var result = await controller.DeleteCourse(1);

            Assert.IsType<ForbidResult>(result);
        }
    }
}
