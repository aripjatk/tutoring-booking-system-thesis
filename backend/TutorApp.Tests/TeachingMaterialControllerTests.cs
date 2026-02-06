using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;
using TutorApp.API.Controllers;
using TutorApp.API.Data;
using TutorApp.API.Interfaces;
using TutorApp.API.DTOs;
using TutorApp.API.Models;
using Xunit;

namespace TutorApp.Tests
{
    public class TeachingMaterialControllerTests
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

        private TeachingMaterialController GetController(TutorDbContext context, string username)
        {
            var mockFileService = new Mock<IFileService>();
            var controller = new TeachingMaterialController(context, mockFileService.Object);

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

            context.TeachingMaterial.Add(new TeachingMaterial
            {
                TeachingMaterialID = 1,
                Name = "Algebra Basics",
                FileName = "algebra.pdf",
                CourseID = 1
            });

            await context.SaveChangesAsync();
        }

        // Checks that a Tutor can retrieve details about a teaching material
        // which is part of a course taught by them.
        [Fact]
        public async Task GetTeachingMaterial_Tutor_CanGetOwn()
        {
            using var context = GetDatabaseContext();
            await SeedData(context);
            var controller = GetController(context, "tutor1");

            var result = await controller.GetTeachingMaterial(1);

            Assert.Equal(1, result.Value.TeachingMaterialID);
            Assert.IsType<TeachingMaterialDto>(result.Value);
        }

        // Checks that a Student can retrieve details about a teaching material
        // which is part of a course they are enrolled in.
        [Fact]
        public async Task GetTeachingMaterial_Student_CanGetEnrolled()
        {
            using var context = GetDatabaseContext();
            await SeedData(context);
            var controller = GetController(context, "student1");

            var result = await controller.GetTeachingMaterial(1);

            Assert.Equal(1, result.Value.TeachingMaterialID);
            Assert.IsType<TeachingMaterialDto>(result.Value);
        }

        // Checks that a Student cannot retrieve details about a teaching material
        // which is part of a course they are not enrolled in.
        [Fact]
        public async Task GetTeachingMaterial_Student_CannotGetUnrelated()
        {
            using var context = GetDatabaseContext();
            await SeedData(context);
            var controller = GetController(context, "student2");

            var result = await controller.GetTeachingMaterial(1);

            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        // Checks that a Tutor can delete a teaching material from a course
        // taught by them.
        [Fact]
        public async Task DeleteTeachingMaterial_Tutor_CanDeleteOwn()
        {
            using var context = GetDatabaseContext();
            await SeedData(context);
            var controller = GetController(context, "tutor1");

            var result = await controller.DeleteTeachingMaterial(1);

            Assert.IsType<NoContentResult>(result);
            Assert.Null(await context.TeachingMaterial.FindAsync(1));
        }


        // Checks that a Student cannot delete a teaching material.
        [Fact]
        public async Task DeleteTeachingMaterial_Student_CannotDelete()
        {
            using var context = GetDatabaseContext();
            await SeedData(context);
            var controller = GetController(context, "student1");

            var result = await controller.DeleteTeachingMaterial(1);

            Assert.IsType<ForbidResult>(result);
        }
    }
}
