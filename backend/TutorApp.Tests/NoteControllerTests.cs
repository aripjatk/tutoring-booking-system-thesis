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
    public class NoteControllerTests
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

        private NoteController GetController(TutorDbContext context, string username)
        {
            var controller = new NoteController(context);

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
            context.Account.Add(new Account { Username = "user1", IsTutor = false, DisplayName = "User One", Email = "user1@example.com" });
            context.Account.Add(new Account { Username = "user2", IsTutor = false, DisplayName = "User Two", Email = "user2@example.com" });

            context.Note.Add(new Note
            {
                NoteID = 1,
                AccountUsername = "user1",
                Body = "User 1 note",
                Date = DateTime.Now
            });

            context.Note.Add(new Note
            {
                NoteID = 2,
                AccountUsername = "user2",
                Body = "User 2 note",
                Date = DateTime.Now
            });

            await context.SaveChangesAsync();
        }

        // Checks that a user can read their own note.
        [Fact]
        public async Task GetNote_User_CanGetOwn()
        {
            using var context = GetDatabaseContext();
            await SeedData(context);
            var controller = GetController(context, "user1");

            var result = await controller.GetNote(1);

            Assert.Equal(1, result.Value.NoteID);
            Assert.IsType<NoteDto>(result.Value);
        }

        // Checks that a user cannot read another user's note.
        [Fact]
        public async Task GetNote_User_CannotGetOther()
        {
            using var context = GetDatabaseContext();
            await SeedData(context);
            var controller = GetController(context, "user1");

            var result = await controller.GetNote(2);

            Assert.IsType<ForbidResult>(result.Result);
        }

        // Checks that a user can create a note connected to their own account.
        [Fact]
        public async Task PostNote_User_CanCreateForSelf()
        {
            using var context = GetDatabaseContext();
            await SeedData(context);
            var controller = GetController(context, "user1");

            var newNote = new NoteCreateDto
            {
                AccountUsername = "user1",
                Body = "Another note",
                Date = DateTime.Now
            };

            var result = await controller.PostNote(newNote);
            var createdAtAction = Assert.IsType<CreatedAtActionResult>(result.Result);
            var createdNote = Assert.IsType<NoteDto>(createdAtAction.Value);

            Assert.Equal("user1", createdNote.AccountUsername);
        }

        // Checks that a user cannot create a note connected to a different user's account.
        [Fact]
        public async Task PostNote_User_CannotCreateForOther()
        {
            using var context = GetDatabaseContext();
            await SeedData(context);
            var controller = GetController(context, "user1");

            var newNote = new NoteCreateDto
            {
                AccountUsername = "user2",
                Body = "Hacking user 2",
                Date = DateTime.Now
            };

            var result = await controller.PostNote(newNote);

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }
    }
}
