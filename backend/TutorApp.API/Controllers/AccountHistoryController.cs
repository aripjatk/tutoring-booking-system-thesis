using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TutorApp.API.Data;
using TutorApp.API.DTOs;
using TutorApp.API.Models;

namespace TutorApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountHistoryController : BaseApiController
    {
        private readonly TutorDbContext _context;

        public AccountHistoryController(TutorDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<AccountHistoryDto>>> GetAccountHistories()
        {
            var username = GetCurrentUsername();
            var histories = await _context.AccountHistory.ToListAsync();

            return Ok(histories.Select(h => new AccountHistoryDto
            {
                HistoryEventID = h.HistoryEventID,
                AccountUsername = h.AccountUsername,
                EventType = h.EventType,
                EventTimestamp = h.EventTimestamp
            }));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<AccountHistoryDto>> GetAccountHistory(int id)
        {
            var username = GetCurrentUsername();
            var accountHistory = await _context.AccountHistory.FindAsync(id);

            if (accountHistory == null)
            {
                return NotFound();
            }

            return new AccountHistoryDto
            {
                HistoryEventID = accountHistory.HistoryEventID,
                AccountUsername = accountHistory.AccountUsername,
                EventType = accountHistory.EventType,
                EventTimestamp = accountHistory.EventTimestamp
            };
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutAccountHistory(int id, AccountHistory accountHistory)
        {
            var username = GetCurrentUsername();
            if (id != accountHistory.HistoryEventID)
            {
                return BadRequest();
            }

            _context.Entry(accountHistory).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AccountHistoryExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        [HttpPost]
        public async Task<ActionResult<AccountHistoryDto>> PostAccountHistory(AccountHistoryCreateDto accountHistoryDto)
        {
            var username = GetCurrentUsername();

            var accountHistory = new AccountHistory
            {
                AccountUsername = accountHistoryDto.AccountUsername,
                EventType = accountHistoryDto.EventType,
                EventTimestamp = accountHistoryDto.EventTimestamp
            };

            _context.AccountHistory.Add(accountHistory);
            await _context.SaveChangesAsync();

            var dto = new AccountHistoryDto
            {
                HistoryEventID = accountHistory.HistoryEventID,
                AccountUsername = accountHistory.AccountUsername,
                EventType = accountHistory.EventType,
                EventTimestamp = accountHistory.EventTimestamp
            };

            return CreatedAtAction("GetAccountHistory", new { id = accountHistory.HistoryEventID }, dto);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAccountHistory(int id)
        {
            var username = GetCurrentUsername();
            var accountHistory = await _context.AccountHistory.FindAsync(id);
            if (accountHistory == null)
            {
                return NotFound();
            }

            _context.AccountHistory.Remove(accountHistory);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool AccountHistoryExists(int id)
        {
            return _context.AccountHistory.Any(e => e.HistoryEventID == id);
        }
    }
}
