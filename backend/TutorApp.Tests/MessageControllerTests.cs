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
    public class MessageControllerTests
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

        private MessageController GetController(TutorDbContext context, string username)
        {
            var mockFileService = new Mock<IFileService>();
            var controller = new MessageController(context, mockFileService.Object);

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
            context.Account.Add(new Account { Username = "user3", IsTutor = false, DisplayName = "User Three", Email = "user3@example.com" });

            context.Message.Add(new Message
            {
                MessageID = 1,
                SenderUsername = "user1",
                RecipientUsername = "user2",
                Topic = "Hello",
                Body = "Hi User 2",
                SentOn = DateTime.Now
            });

            await context.SaveChangesAsync();
        }

        // Checks that a user can send messages to another user.
        [Fact]
        public async Task PostMessage_User_CanSendToOther()
        {
            using var context = GetDatabaseContext();
            await SeedData(context);
            var controller = GetController(context, "user1");

            var newMessage = new MessageCreateDto
            {
                RecipientUsername = "user2",
                Topic = "New Message",
                Body = "Content"
            };

            var result = await controller.PostMessage(newMessage);
            var createdAtAction = Assert.IsType<CreatedAtActionResult>(result.Result);
            var createdMessage = Assert.IsType<MessageDto>(createdAtAction.Value);

            Assert.Equal("user1", createdMessage.SenderUsername);
            Assert.Equal("user2", createdMessage.RecipientUsername);
        }

        // Checks that a user cannot send a message to themself.
        [Fact]
        public async Task PostMessage_User_CannotSendToSelf()
        {
            using var context = GetDatabaseContext();
            await SeedData(context);
            var controller = GetController(context, "user1");

            var newMessage = new MessageCreateDto
            {
                RecipientUsername = "user1",
                Topic = "New Message",
                Body = "Content"
            };

            var result = await controller.PostMessage(newMessage);

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        // Checks that a user can read a message they sent.
        [Fact]
        public async Task GetMessage_Sender_CanRead()
        {
            using var context = GetDatabaseContext();
            await SeedData(context);
            var controller = GetController(context, "user1");

            var result = await controller.GetMessage(1);

            Assert.Equal(1, result.Value.MessageID);
            Assert.IsType<MessageDto>(result.Value);
        }

        // Checks that a user can read a message they received.
        [Fact]
        public async Task GetMessage_Recipient_CanRead()
        {
            using var context = GetDatabaseContext();
            await SeedData(context);
            var controller = GetController(context, "user2");

            var result = await controller.GetMessage(1);

            Assert.Equal(1, result.Value.MessageID);
            Assert.IsType<MessageDto>(result.Value);
        }

        // Checks that a user cannot read a message if they are not the
        // sender or recipient of the message.
        [Fact]
        public async Task GetMessage_Other_CannotRead()
        {
            using var context = GetDatabaseContext();
            await SeedData(context);
            var controller = GetController(context, "user3");

            var result = await controller.GetMessage(1);

            Assert.IsType<ForbidResult>(result.Result);
        }
    }
}
