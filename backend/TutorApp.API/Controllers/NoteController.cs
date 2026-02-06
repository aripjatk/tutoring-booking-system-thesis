using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TutorApp.API.Data;
using TutorApp.API.DTOs;
using TutorApp.API.Models;

namespace TutorApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NoteController : BaseApiController
    {
        private readonly TutorDbContext _context;

        public NoteController(TutorDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<NoteDto>>> GetNotes()
        {
            var username = GetCurrentUsername();
            var notes = await _context.Note.Where(x => x.AccountUsername.Equals(username)).ToListAsync();

            return Ok(notes.Select(n => new NoteDto
            {
                NoteID = n.NoteID,
                AccountUsername = n.AccountUsername,
                Date = n.Date,
                Body = n.Body
            }));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<NoteDto>> GetNote(int id)
        {
            var username = GetCurrentUsername();
            var note = await _context.Note.FindAsync(id);

            if (note == null)
                return NotFound();

            if (!await NoteBelongsToAsync(id, username))
                return Forbid("Requested note does not belong to current user");

            return new NoteDto
            {
                NoteID = note.NoteID,
                AccountUsername = note.AccountUsername,
                Date = note.Date,
                Body = note.Body
            };
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutNote(int id, NoteDto note)
        {
            var username = GetCurrentUsername();

            if (id != note.NoteID)
                return BadRequest("Note ID in request path does not match note ID in request body");
            var existingNote = await _context.Note.FindAsync(id);
            if (existingNote == null)
                return BadRequest("No such note");

            if (!await NoteBelongsToAsync(id, username))
                return Forbid("Cannot update notes belonging to other users");
            if (note.AccountUsername != existingNote.AccountUsername)
                return Forbid("Cannot change the user to whom a note belongs");

            existingNote.Body = note.Body;
            existingNote.Date = note.Date;
            
            _context.Entry(existingNote).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await NoteExistsAsync(id))
                    return NotFound();
                else
                    throw;
            }

            return NoContent();
        }

        [HttpPost]
        public async Task<ActionResult<NoteDto>> PostNote(NoteCreateDto noteDto)
        {
            var username = GetCurrentUsername();
            if (!(noteDto.AccountUsername.Equals(username)))
                return BadRequest("Attempted to create a note for another user");

            var note = new Note
            {
                AccountUsername = noteDto.AccountUsername,
                Date = noteDto.Date,
                Body = noteDto.Body
            };

            _context.Note.Add(note);
            await _context.SaveChangesAsync();

            var dto = new NoteDto
            {
                NoteID = note.NoteID,
                AccountUsername = note.AccountUsername,
                Date = note.Date,
                Body = note.Body
            };

            return CreatedAtAction("GetNote", new { id = note.NoteID }, dto);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNote(int id)
        {
            var username = GetCurrentUsername();
            var note = await _context.Note.FindAsync(id);
            if (note == null)
                return NotFound();

            if (!await NoteBelongsToAsync(id, username))
                return Forbid("Cannot delete notes belonging to other users");

            _context.Note.Remove(note);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private async Task<bool> NoteExistsAsync(int id)
        {
            return await _context.Note.AnyAsync(e => e.NoteID == id);
        }

        private async Task<bool> NoteBelongsToAsync(int id, string username) {
            var note = await _context.Note.FindAsync(id);
            if (note == null)
                throw new ArgumentException("No note with id " + id);
            return note.AccountUsername.Equals(username);
        }
    }
}
