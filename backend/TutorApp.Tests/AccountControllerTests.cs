using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
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
    public class AccountControllerTests
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

        private AccountController GetController(TutorDbContext context,
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
                mockConfig.Setup(c => c["TokenKey"]).Returns("super secret key");
            }

            return new AccountController(context, mockTokenService.Object, mockEmailService.Object, mockConfig.Object);
        }

        // Checks that the Register endpoint for creating a tutor account sends the user an email with an activation link.
        [Fact]
        public async Task Register_ShouldCreateInactiveAccount_AndSendEmail()
        {
            using var context = GetDatabaseContext();
            var mockEmailService = new Mock<IEmailService>();
            var controller = GetController(context, mockEmailService: mockEmailService);

            var registerDto = new RegisterDto
            {
                Username = "testuser",
                Password = "password",
                Email = "test@example.com",
                DisplayName = "Test User"
            };

            var result = await controller.RegisterTutor(registerDto);

            var okResult = Assert.IsType<OkResult>(result.Result);

            var account = await context.Account.Include(a => a.AccountSettings).FirstOrDefaultAsync(u => u.Username == "testuser");
            Assert.NotNull(account);
            Assert.False(account.IsActive);
            Assert.False(string.IsNullOrEmpty(account.AccountSettings.ActivationToken));
            Assert.True(account.AccountSettings.TokenExpirationDate > DateTime.Now);

            mockEmailService.Verify(e => e.SendEmailAsync("test@example.com", "Verify your email", It.Is<string>(s => s.Contains("token=") && s.Contains("username=testuser"))), Times.Once);
        }

        // Checks that the Login endpoint cannot be called if the account is inactive
        // (i.e. has not been activated using the e-mail link)
        [Fact]
        public async Task Login_ShouldFail_IfAccountInactive()
        {
            using var context = GetDatabaseContext();

            using var hmac = new HMACSHA512();
            var account = new Account
            {
                Username = "inactive",
                DisplayName = "Inactive User",
                Email = "inactive@test.com",
                IsActive = false,
                AccountSettings = new AccountSettings
                {
                    AccountUsername = "inactive",
                    PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes("password")),
                    PasswordSalt = hmac.Key,
                    TokenExpirationDate = DateTime.Now.AddDays(1),
                    ActivationToken = "somehash",
                    ProfilePictureFileName = ""
                }
            };
            context.Account.Add(account);
            await context.SaveChangesAsync();

            var controller = GetController(context);
            var loginDto = new LoginDto { Username = "inactive", Password = "password" };

            var result = await controller.Login(loginDto);

            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
            Assert.Equal("Account is not active. Please verify your email.", unauthorizedResult.Value);
        }

        // Checks that the activation link from the e-mail activates the user’s account when clicked.
        [Fact]
        public async Task VerifyEmail_ShouldActivateAccount_WithValidToken()
        {
            using var context = GetDatabaseContext();
            var controller = GetController(context);

            var tokenRaw = "MySecretToken";
            using var sha256 = SHA256.Create();
            var tokenHash = Convert.ToHexString(sha256.ComputeHash(Encoding.UTF8.GetBytes(tokenRaw)));

            var account = new Account
            {
                Username = "verifiable",
                DisplayName = "Verifiable User",
                Email = "verifiable@test.com",
                IsActive = false,
                AccountSettings = new AccountSettings
                {
                    AccountUsername = "verifiable",
                    ActivationToken = tokenHash,
                    TokenExpirationDate = DateTime.Now.AddHours(1),
                    PasswordHash = new byte[64],
                    PasswordSalt = new byte[128],
                    ProfilePictureFileName = ""
                }
            };
            context.Account.Add(account);
            await context.SaveChangesAsync();

            var result = await controller.VerifyEmail("verifiable", tokenRaw);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Contains("verified successfully", okResult.Value.ToString());

            var dbAccount = await context.Account.FindAsync("verifiable");
            Assert.True(dbAccount.IsActive);
            Assert.Empty(dbAccount.AccountSettings.ActivationToken);
        }

        // Checks that attempting to activate an account with an incorrect token results in an error.
        [Fact]
        public async Task VerifyEmail_ShouldFail_WithInvalidToken()
        {
            using var context = GetDatabaseContext();
            var controller = GetController(context);

            var account = new Account
            {
                Username = "badtoken",
                DisplayName = "Bad Token User",
                Email = "badtoken@test.com",
                IsActive = false,
                AccountSettings = new AccountSettings
                {
                    AccountUsername = "badtoken",
                    ActivationToken = "SomeHash",
                    TokenExpirationDate = DateTime.Now.AddHours(1),
                    PasswordHash = new byte[64],
                    PasswordSalt = new byte[128],
                    ProfilePictureFileName = ""
                }
            };
            context.Account.Add(account);
            await context.SaveChangesAsync();

            var result = await controller.VerifyEmail("badtoken", "WrongToken");

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Invalid token", badRequestResult.Value);
        }

        // Checks that attempting to activate an account with an expired token results in an error.
        [Fact]
        public async Task VerifyEmail_ShouldFail_IfExpired()
        {
            using var context = GetDatabaseContext();
            var controller = GetController(context);

            var tokenRaw = "MySecretToken";
            using var sha256 = SHA256.Create();
            var tokenHash = Convert.ToHexString(sha256.ComputeHash(Encoding.UTF8.GetBytes(tokenRaw)));

            var account = new Account
            {
                Username = "expired",
                DisplayName = "Expired User",
                Email = "expired@test.com",
                IsActive = false,
                AccountSettings = new AccountSettings
                {
                    AccountUsername = "expired",
                    ActivationToken = tokenHash,
                    TokenExpirationDate = DateTime.Now.AddHours(-1), // Expired
                    PasswordHash = new byte[64],
                    PasswordSalt = new byte[128],
                    ProfilePictureFileName = ""
                }
            };
            context.Account.Add(account);
            await context.SaveChangesAsync();

            var result = await controller.VerifyEmail("expired", tokenRaw);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Token expired", badRequestResult.Value);
        }
    }
}
