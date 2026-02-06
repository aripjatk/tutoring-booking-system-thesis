using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using TutorApp.API.Data;
using TutorApp.API.DTOs;
using TutorApp.API.Interfaces;
using TutorApp.API.Models;

namespace TutorApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : BaseApiController
    {
        private readonly TutorDbContext _dbcontext;
        private readonly ITokenService _tokenService;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _config;

        public AccountController(TutorDbContext context, ITokenService tokenService, IEmailService emailService, IConfiguration config)
        {
            _dbcontext = context;
            _tokenService = tokenService;
            _emailService = emailService;
            _config = config;
        }

        private async Task CreateAccountAsync(RegisterDto registerDto, bool isTutor) {
            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(_config["TokenKey"]));

            var account = new Account {
                Username = registerDto.Username,
                DisplayName = registerDto.DisplayName,
                Email = registerDto.Email,
                IsActive = false,
                IsTutor = isTutor
            };

            var tokenBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(tokenBytes);
            }
            var activationToken = Convert.ToHexString(tokenBytes);

            using var sha256 = SHA256.Create();
            var activationTokenHash = Convert.ToHexString(sha256.ComputeHash(Encoding.UTF8.GetBytes(activationToken)));

            var accountSettings = new AccountSettings
            {
                AccountUsername = registerDto.Username,
                PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDto.Password)),
                PasswordSalt = hmac.Key,
                ActivationToken = activationTokenHash,
                TokenExpirationDate = DateTime.Now.AddHours(24),
                ProfilePictureFileName = string.Empty
            };

            account.AccountSettings = accountSettings;
            _dbcontext.Account.Add(account);
            _dbcontext.AccountSettings.Add(accountSettings);

            await _dbcontext.SaveChangesAsync();

            var baseUrl = _config["AppUrl"];
            var verificationLink = $"{baseUrl}/api/account/verify-email?username={Uri.EscapeDataString(account.Username)}&token={activationToken}";
            var emailBody = $"<p>Please click the link below to verify your email:</p><p><a href='{verificationLink}'>Verify Email</a></p>";

            await _emailService.SendEmailAsync(account.Email, "Verify your email", emailBody);
        }

        [HttpPost("registerStudent")]
        public async Task<ActionResult<UserDto>> RegisterStudent(RegisterDto registerDto)
        {
            var tutorUsername = GetCurrentUsername();
            var account = await _dbcontext.Account.FindAsync(tutorUsername);
            if (account == null)
                return Unauthorized(ErrorMessages.UserNotFound);
            if (!account.IsTutor)
                return Forbid(ErrorMessages.NotATutor);

            if (await UsernameExistsAsync(registerDto.Username))
                return BadRequest(ErrorMessages.UsernameTaken);

            if (await EmailExistsAsync(registerDto.Email))
                return BadRequest(ErrorMessages.EmailTaken);

            await CreateAccountAsync(registerDto, false);
            return Ok();
        }

        [AllowAnonymous]
        [HttpPost("registerTutor")]
        public async Task<ActionResult<UserDto>> RegisterTutor(RegisterDto registerDto)
        {
            try {
                if (await UsernameExistsAsync(registerDto.Username))
                    return BadRequest(ErrorMessages.UsernameTaken);

                if (await EmailExistsAsync(registerDto.Email))
                    return BadRequest(ErrorMessages.EmailTaken);

                await CreateAccountAsync(registerDto, true);
                return Ok();
            } catch(Exception ex) {
                var inner = ex.InnerException == null ? "" : "Inner: " + ex.InnerException.Message;
                return StatusCode(500, "Cannot create account due to server error: " + ex.Message + inner);
            }
        }

        [AllowAnonymous]
        [HttpGet("verify-email")]
        public async Task<IActionResult> VerifyEmail(string username, string token)
        {
            var account = await _dbcontext.Account
                .Include(x => x.AccountSettings)
                .SingleOrDefaultAsync(x => x.Username == username);

            if (account == null)
                return BadRequest("Invalid username");

            if (account.IsActive)
                return Ok("Account already activated.");

            if (account.AccountSettings == null)
                return BadRequest("Account settings not found");

            if (DateTime.Now > account.AccountSettings.TokenExpirationDate)
                return BadRequest("Token expired");

            using var sha256 = SHA256.Create();
            var tokenHash = Convert.ToHexString(sha256.ComputeHash(Encoding.UTF8.GetBytes(token)));

            if (tokenHash != account.AccountSettings.ActivationToken)
                return BadRequest("Invalid token");

            account.IsActive = true;
            account.AccountSettings.ActivationToken = string.Empty;

            await _dbcontext.SaveChangesAsync();

            return Ok("Email verified successfully. You can now login.");
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
        {
            try {
                var account = await _dbcontext.Account
                    .Include(x => x.AccountSettings)
                    .Include(x => x.AccountHistory)
                    .SingleOrDefaultAsync(x => x.Username == loginDto.Username);

                if (account == null)
                    return Unauthorized("Invalid username");

                if (!account.IsActive)
                    return Unauthorized("Account is not active. Please verify your email.");

                if (account.AccountSettings == null)
                    throw new MissingMemberException("No account settings for given account - DB corrupted?");

                using var hmac = new HMACSHA512(account.AccountSettings.PasswordSalt);

                var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Password));

                for (int i = 0; i < computedHash.Length; i++) {
                    if (computedHash[i] != account.AccountSettings.PasswordHash[i])
                        return Unauthorized("Invalid password");
                }

                var lastHistoryEvent = account.AccountHistory
                    .OrderByDescending(h => h.EventTimestamp)
                    .FirstOrDefault();

                if (lastHistoryEvent != null && lastHistoryEvent.EventType == EventType.Deactivation) {
                    var activationEvent = new AccountHistory {
                        AccountUsername = account.Username,
                        EventType = EventType.Activation,
                        EventTimestamp = DateTime.Now
                    };
                    _dbcontext.AccountHistory.Add(activationEvent);
                    await _dbcontext.SaveChangesAsync();
                }

                return new UserDto {
                    Username = account.Username,
                    Token = _tokenService.CreateToken(account)
                };
            } catch(Exception ex) {
                var inner = ex.InnerException == null ? "" : ex.InnerException.Message;
                return StatusCode(500, "Cannot log in because of a server error: " + ex.Message + inner);
            }
        }

        [HttpPost("deactivate/{username}")]
        public async Task<IActionResult> DeactivateAccount(string username)
        {
            var currentUsername = GetCurrentUsername();
            var currentUser = await _dbcontext.Account.FindAsync(currentUsername);

            if (currentUser == null)
                return Unauthorized();

            if (!currentUser.IsTutor)
                return Forbid("Only tutors can deactivate accounts.");

            Account targetAccount = null;

            if (currentUsername == username)
            {
                targetAccount = currentUser;
            }
            else
            {
                targetAccount = await _dbcontext.Account.FindAsync(username);
                if (targetAccount == null)
                    return NotFound("Target user not found.");

                if (targetAccount.IsTutor)
                    return Forbid("Tutors cannot deactivate other tutors.");
            }

            var historyEvent = new AccountHistory
            {
                AccountUsername = username,
                EventType = EventType.Deactivation,
                EventTimestamp = DateTime.Now
            };

            _dbcontext.AccountHistory.Add(historyEvent);
            await _dbcontext.SaveChangesAsync();

            if (!targetAccount.IsTutor)
            {
                var emailBody = $"<p>Your account has been deactivated. It will be permanently deleted in 2 weeks unless you log in to reactivate it.</p>";
                await _emailService.SendEmailAsync(targetAccount.Email, "Account Deactivated", emailBody);
            }

            return Ok("Account deactivated successfully.");
        }

        private async Task<bool> UsernameExistsAsync(string username)
        {
            return await _dbcontext.Account.AnyAsync(x => x.Username == username);
        }

        private async Task<bool> EmailExistsAsync(string email) {
            return await _dbcontext.Account.AnyAsync(x => x.Email == email);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<AccountDto>>> GetAccounts()
        {
            var currentUsername = GetCurrentUsername();
            var currentUser = await _dbcontext.Account.FindAsync(currentUsername);

            IEnumerable<Account> accounts;

            if (currentUser != null && currentUser.IsTutor)
            {
                accounts = await _dbcontext.Account.ToListAsync();
            }
            else
            {
                accounts = await _dbcontext.Account.Where(a => a.Username == currentUsername).ToListAsync();
            }

            return Ok(accounts.Select(a => new AccountDto
            {
                Username = a.Username,
                DisplayName = a.DisplayName,
                Email = a.Email,
                IsTutor = a.IsTutor,
                IsActive = a.IsActive
            }));
        }

        [HttpGet("{username}")]
        public async Task<ActionResult<AccountDto>> GetAccount(string username)
        {
            var currentUsername = GetCurrentUsername();
            var currentUser = await _dbcontext.Account.FindAsync(currentUsername);

            if (currentUser != null && (currentUser.IsTutor || currentUsername == username))
            {
                var account = await _dbcontext.Account.FindAsync(username);

                if (account == null)
                    return NotFound();

                return Ok(new AccountDto {
                    Username = account.Username,
                    IsTutor = account.IsTutor,
                    DisplayName = account.DisplayName,
                    Email = account.Email,
                    IsActive = account.IsActive
                });
            }

            return Forbid();
        }

        private bool AccountExists(string username)
        {
            return _dbcontext.Account.Any(e => e.Username == username);
        }
    }
}
