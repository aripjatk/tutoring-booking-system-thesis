using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using TutorApp.API.Controllers;
using TutorApp.API.Data;
using TutorApp.API.DTOs;
using TutorApp.API.Interfaces;
using TutorApp.API.Models;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace TutorApp.Tests
{
    public class AccountDeactivationTests
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

        private AccountController GetController(TutorDbContext context, string currentUsername,
            Mock<ITokenService> mockTokenService = null,
            Mock<IEmailService> mockEmailService = null,
            Mock<IConfiguration> mockConfig = null)
        {
            if (mockTokenService == null) mockTokenService = new Mock<ITokenService>();
            if (mockEmailService == null) mockEmailService = new Mock<IEmailService>();
            if (mockConfig == null)
            {
                mockConfig = new Mock<IConfiguration>();
                mockConfig.Setup(c => c["AppUrl"]).Returns("http://test.com");
            }

            var controller = new AccountController(context, mockTokenService.Object, mockEmailService.Object, mockConfig.Object);

            // Mock User
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, currentUsername),
            }, "mock"));

            controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = user }
            };

            return controller;
        }

        private void CreateAccount(TutorDbContext context, string username, bool isTutor)
        {
            using var hmac = new HMACSHA512();
            var account = new Account
            {
                Username = username,
                DisplayName = username,
                Email = $"{username}@test.com",
                IsActive = true,
                IsTutor = isTutor,
                AccountSettings = new AccountSettings
                {
                    AccountUsername = username,
                    PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes("password")),
                    PasswordSalt = hmac.Key,
                    TokenExpirationDate = DateTime.Now.AddDays(1),
                    ActivationToken = "",
                    ProfilePictureFileName = ""
                }
            };
            context.Account.Add(account);
            context.SaveChanges();
        }

        [Fact]
        public async Task DeactivateAccount_TutorDeactivatesStudent_Success_AndEmailSent()
        {
            using var context = GetDatabaseContext();
            CreateAccount(context, "tutor1", true);
            CreateAccount(context, "student1", false);

            var mockEmailService = new Mock<IEmailService>();
            var controller = GetController(context, "tutor1", mockEmailService: mockEmailService);

            var result = await controller.DeactivateAccount("student1");

            Assert.IsType<OkObjectResult>(result);

            // Verify History
            var history = await context.AccountHistory.Where(h => h.AccountUsername == "student1").ToListAsync();
            Assert.Single(history);
            Assert.Equal(EventType.Deactivation, history.First().EventType);

            // Verify Email
            mockEmailService.Verify(e => e.SendEmailAsync("student1@test.com", "Account Deactivated", It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task DeactivateAccount_TutorDeactivatesSelf_Success_NoEmail()
        {
            using var context = GetDatabaseContext();
            CreateAccount(context, "tutor1", true);

            var mockEmailService = new Mock<IEmailService>();
            var controller = GetController(context, "tutor1", mockEmailService: mockEmailService);

            var result = await controller.DeactivateAccount("tutor1");

            Assert.IsType<OkObjectResult>(result);

            var history = await context.AccountHistory.Where(h => h.AccountUsername == "tutor1").ToListAsync();
            Assert.Single(history);
            Assert.Equal(EventType.Deactivation, history.First().EventType);

            // Verify No Email for self (Tutor)
            mockEmailService.Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task DeactivateAccount_TutorDeactivatesOtherTutor_Forbid()
        {
            using var context = GetDatabaseContext();
            CreateAccount(context, "tutor1", true);
            CreateAccount(context, "tutor2", true);

            var controller = GetController(context, "tutor1");

            var result = await controller.DeactivateAccount("tutor2");

            var forbidResult = Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task DeactivateAccount_StudentDeactivatesSelf_Forbid()
        {
            using var context = GetDatabaseContext();
            CreateAccount(context, "student1", false);

            var controller = GetController(context, "student1");

            var result = await controller.DeactivateAccount("student1");
            var forbidResult = Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task DeactivateAccount_StudentDeactivatesTutor_Forbid()
        {
            using var context = GetDatabaseContext();
            CreateAccount(context, "student", false);
            CreateAccount(context, "tutor", true);

            var controller = GetController(context, "student");
            var result = await controller.DeactivateAccount("tutor");
            var forbidResult = Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task Login_ReactivatesAccount_IfDeactivated()
        {
             using var context = GetDatabaseContext();
             CreateAccount(context, "student1", false);

             context.AccountHistory.Add(new AccountHistory
             {
                 AccountUsername = "student1",
                 EventType = EventType.Deactivation,
                 EventTimestamp = DateTime.Now.AddDays(-1)
             });
             context.SaveChanges();

             var controller = GetController(context, "student1");

             var loginDto = new LoginDto { Username = "student1", Password = "password" };
             var result = await controller.Login(loginDto);

             Assert.IsType<UserDto>(result.Value);

             var history = await context.AccountHistory
                 .Where(h => h.AccountUsername == "student1")
                 .OrderByDescending(h => h.EventTimestamp)
                 .ToListAsync();

             Assert.Equal(2, history.Count);
             Assert.Equal(EventType.Activation, history.First().EventType);
        }
    }
}
