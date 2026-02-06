using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TutorApp.API.Data;
using TutorApp.API.DTOs;
using TutorApp.API.Models;

namespace TutorApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountSettingsController : BaseApiController
    {
        private readonly TutorDbContext _context;

        public AccountSettingsController(TutorDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<AccountSettingsDto>> GetAccountSettings()
        {
            var username = GetCurrentUsername();
            var settings = await _context.AccountSettings.FindAsync(username);
            if (settings == null)
                return StatusCode(500, "No account settings for given account - DB corrupted?");
            else
                return new AccountSettingsDto
                {
                    AccountUsername = settings.AccountUsername,
                    TokenExpirationDate = settings.TokenExpirationDate,
                    ProfilePictureFileName = settings.ProfilePictureFileName
                };
        }

        [HttpGet("{username}")]
        public async Task<ActionResult<AccountSettingsDto>> GetAccountSetting(string username)
        {
            var currentUsername = GetCurrentUsername();
            var currentAccount = await _context.Account.FindAsync(currentUsername);

            if ((currentUsername != username) && !(currentAccount.IsTutor))
                return Forbid("Current account is not allowed to retrieve settings for other accounts");

            var requestedAccount = await _context.Account.FindAsync(username);
            if (requestedAccount == null)
                return NotFound("No such account");
            var accountSettings = requestedAccount.AccountSettings;

            if (accountSettings == null)
                return StatusCode(500, "No account settings for given account - DB corrupted?");

            return new AccountSettingsDto
            {
                AccountUsername = accountSettings.AccountUsername,
                TokenExpirationDate = accountSettings.TokenExpirationDate,
                ProfilePictureFileName = accountSettings.ProfilePictureFileName
            };
        }

        [HttpPut("{username}")]
        public async Task<IActionResult> PutAccountSetting(string username, AccountSettings accountSetting)
        {
            var currentUsername = GetCurrentUsername();
            username = currentUsername;
            if (username != accountSetting.AccountUsername)
                return Forbid("Cannot change settings for other accounts");

            _context.Entry(accountSetting).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AccountSettingsExist(username))
                    return NotFound();
                else
                    throw;
            }

            return NoContent();
        }

        private bool AccountSettingsExist(string username)
        {
            return _context.AccountSettings.Any(e => e.AccountUsername == username);
        }
    }
}
