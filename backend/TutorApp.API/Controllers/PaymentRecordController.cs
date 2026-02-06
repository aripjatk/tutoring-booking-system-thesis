using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TutorApp.API.Data;
using TutorApp.API.DTOs;
using TutorApp.API.Models;

namespace TutorApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentRecordController : BaseApiController
    {
        private readonly TutorDbContext _context;

        public PaymentRecordController(TutorDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PaymentRecordDto>>> GetPaymentRecords()
        {
            var username = GetCurrentUsername();
            var account = await _context.Account.FindAsync(username);

            if (account == null)
                return Unauthorized(ErrorMessages.UserNotFound);

            List<PaymentRecord> records;

            if(account.IsTutor)
                records = await _context.PaymentRecord.Where(x => x.TutorUsername == username).ToListAsync();
            else
                records = await _context.PaymentRecord.Where(x => x.StudentUsername == username).ToListAsync();

            return Ok(records.Select(r => new PaymentRecordDto
            {
                PaymentRecordID = r.PaymentRecordID,
                StudentUsername = r.StudentUsername,
                TutorUsername = r.TutorUsername,
                AmountPaid = r.AmountPaid,
                MeansOfPayment = r.MeansOfPayment,
                PaidOn = r.PaidOn
            }));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<PaymentRecordDto>> GetPaymentRecord(int id)
        {
            var username = GetCurrentUsername();
            var paymentRecord = await _context.PaymentRecord.FindAsync(id);
            if (paymentRecord == null)
                return NotFound();

            if (!paymentRecord.TutorUsername.Equals(username) && !paymentRecord.StudentUsername.Equals(username))
                return Forbid();

            return new PaymentRecordDto
            {
                PaymentRecordID = paymentRecord.PaymentRecordID,
                StudentUsername = paymentRecord.StudentUsername,
                TutorUsername = paymentRecord.TutorUsername,
                AmountPaid = paymentRecord.AmountPaid,
                MeansOfPayment = paymentRecord.MeansOfPayment,
                PaidOn = paymentRecord.PaidOn
            };
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutPaymentRecord(int id, PaymentRecordDto paymentRecordDto)
        {
            var username = GetCurrentUsername();
            if (id != paymentRecordDto.PaymentRecordID)
                return BadRequest();

            var account = await _context.Account.FindAsync(username);
            if (account == null || !account.IsTutor)
                return Forbid();

            var existingRecord = await _context.PaymentRecord.AsNoTracking().FirstOrDefaultAsync(p => p.PaymentRecordID == id);

            if (existingRecord == null)
                return NotFound();

            if (existingRecord.TutorUsername != username)
                return Forbid("Cannot update payment records for other tutors");
            if (paymentRecordDto.TutorUsername != username ||
                paymentRecordDto.StudentUsername != existingRecord.StudentUsername)
                    return Forbid("Cannot change the username of the tutor or student in a payment record");

            existingRecord.MeansOfPayment = paymentRecordDto.MeansOfPayment;
            existingRecord.PaidOn = paymentRecordDto.PaidOn;
            existingRecord.AmountPaid = paymentRecordDto.AmountPaid;

            _context.Entry(existingRecord).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PaymentRecordExists(id))
                    return NotFound();
                else
                    throw;
            }

            return NoContent();
        }

        [HttpPost]
        public async Task<ActionResult<PaymentRecordDto>> PostPaymentRecord(PaymentRecordCreateDto paymentRecordDto)
        {
            var username = GetCurrentUsername();
            var account = await _context.Account.FindAsync(username);
            if (account == null)
                return Unauthorized(ErrorMessages.UserNotFound);

            if(!account.IsTutor)
                return Forbid(ErrorMessages.NotATutor);

            if (paymentRecordDto.TutorUsername != username)
                return BadRequest();

            var paymentRecord = new PaymentRecord
            {
                StudentUsername = paymentRecordDto.StudentUsername,
                TutorUsername = paymentRecordDto.TutorUsername,
                AmountPaid = paymentRecordDto.AmountPaid,
                MeansOfPayment = paymentRecordDto.MeansOfPayment,
                PaidOn = paymentRecordDto.PaidOn
            };

            _context.PaymentRecord.Add(paymentRecord);
            await _context.SaveChangesAsync();

            var dto = new PaymentRecordDto
            {
                PaymentRecordID = paymentRecord.PaymentRecordID,
                StudentUsername = paymentRecord.StudentUsername,
                TutorUsername = paymentRecord.TutorUsername,
                AmountPaid = paymentRecord.AmountPaid,
                MeansOfPayment = paymentRecord.MeansOfPayment,
                PaidOn = paymentRecord.PaidOn
            };

            return CreatedAtAction("GetPaymentRecord", new { id = paymentRecord.PaymentRecordID }, dto);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePaymentRecord(int id)
        {
            var username = GetCurrentUsername();
            var account = await _context.Account.FindAsync(username);
            if (account == null)
                return Unauthorized(ErrorMessages.UserNotFound);

            if (!account.IsTutor)
                return Forbid(ErrorMessages.NotATutor);

            var paymentRecord = await _context.PaymentRecord.FindAsync(id);
            if (paymentRecord == null)
                return NotFound("No such payment record");

            if (paymentRecord.TutorUsername != username)
                return Forbid();

            _context.PaymentRecord.Remove(paymentRecord);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool PaymentRecordExists(int id)
        {
            return _context.PaymentRecord.Any(e => e.PaymentRecordID == id);
        }
    }
}
