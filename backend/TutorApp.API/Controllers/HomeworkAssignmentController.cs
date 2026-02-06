using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TutorApp.API.Data;
using TutorApp.API.DTOs;
using TutorApp.API.Interfaces;
using TutorApp.API.Models;

namespace TutorApp.API.Controllers {
    [Route("api/[controller]")]
    [ApiController]
    public class HomeworkAssignmentController : BaseApiController
    {
        private readonly TutorDbContext _context;
        private readonly IFileService _fileService;

        public HomeworkAssignmentController(TutorDbContext context, IFileService fileService)
        {
            _context = context;
            _fileService = fileService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<HomeworkAssignmentDto>>> GetHomeworkAssignments()
        {
            var username = GetCurrentUsername();
            var account = await _context.Account.FindAsync(username);
            if (account == null)
                return Unauthorized(ErrorMessages.UserNotFound);

            IQueryable<HomeworkAssignment> query = _context.HomeworkAssignment
                .Include(h => h.Session)
                .ThenInclude(s => s.Course);

            if (account.IsTutor)
                query = query.Where(h => h.Session.Course.TutorUsername == username);
            else
                query = query.Where(h => h.Session.StudentUsername == username);

            var assignments = await query.ToListAsync();

            return Ok(assignments.Select(h => new HomeworkAssignmentDto
            {
                HomeworkAssignmentID = h.HomeworkAssignmentID,
                SessionID = h.SessionID,
                Name = h.Name,
                Objective = h.Objective,
                HasSolutionFile = h.SolutionFileName != null,
                SolutionFeedback = h.SolutionFeedback
            }));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<HomeworkAssignmentDto>> GetHomeworkAssignment(int id)
        {
            var username = GetCurrentUsername();
            var account = await _context.Account.FindAsync(username);
            if (account == null)
                return Unauthorized(ErrorMessages.UserNotFound);

            var homeworkAssignment = await _context.HomeworkAssignment
                .Include(h => h.Session)
                .ThenInclude(s => s.Course)
                .FirstOrDefaultAsync(h => h.HomeworkAssignmentID == id);

            if (homeworkAssignment == null)
                return NotFound();

            if (account.IsTutor)
            {
                if (homeworkAssignment.Session.Course.TutorUsername != username)
                    return Forbid("Attempted to access homework for a course created by a different tutor");
            }
            else
            {
                if (homeworkAssignment.Session.StudentUsername != username)
                    return Forbid("Attempted to access homework assigned to a different student");
            }

            return new HomeworkAssignmentDto
            {
                HomeworkAssignmentID = homeworkAssignment.HomeworkAssignmentID,
                SessionID = homeworkAssignment.SessionID,
                Name = homeworkAssignment.Name,
                Objective = homeworkAssignment.Objective,
                HasSolutionFile = homeworkAssignment.SolutionFileName != null,
                SolutionFeedback = homeworkAssignment.SolutionFeedback
            };
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutHomeworkAssignment(int id, HomeworkAssignmentEditDto editDto)
        {
            var username = GetCurrentUsername();
            var account = await _context.Account.FindAsync(username);
            if (account == null)
                return Unauthorized(ErrorMessages.UserNotFound);

            if (!account.IsTutor)
                return Forbid(ErrorMessages.NotATutor);

            var existingAssignment = await _context.HomeworkAssignment
                .Include(h => h.Session)
                .ThenInclude(s => s.Course)
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.HomeworkAssignmentID == id);

            if (existingAssignment == null)
                return NotFound("No such assignment");

            if (existingAssignment.Session.Course.TutorUsername != username)
                return Forbid("Attempted to edit a homework assignment belonging to a different tutor");

            existingAssignment.Name = editDto.Name;
            existingAssignment.Objective = editDto.Objective;
            existingAssignment.SolutionFeedback = editDto.SolutionFeedback;

            _context.Entry(existingAssignment).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await HomeworkAssignmentExistsAsync(id))
                    return NotFound();
                else
                    throw;
            }

            return NoContent();
        }

        [HttpPost]
        public async Task<ActionResult<HomeworkAssignmentDto>> PostHomeworkAssignment([FromForm] HomeworkAssignmentCreateDto homeworkAssignmentDto)
        {
            var username = GetCurrentUsername();
            var account = await _context.Account.FindAsync(username);
            if (account == null)
                return Unauthorized(ErrorMessages.UserNotFound);

            if (!account.IsTutor)
                return Forbid(ErrorMessages.NotATutor);

            var session = await _context.Session
                .Include(s => s.Course)
                .FirstOrDefaultAsync(s => s.SessionID == homeworkAssignmentDto.SessionID);

            if (session == null)
                return NotFound("Cannot find session with given ID");

            if (session.Course.TutorUsername != username)
                return Forbid("Session is part of a course belonging to a different tutor");

            var homeworkAssignment = new HomeworkAssignment
            {
                SessionID = homeworkAssignmentDto.SessionID,
                Name = homeworkAssignmentDto.Name,
                Objective = homeworkAssignmentDto.Objective
            };

            _context.HomeworkAssignment.Add(homeworkAssignment);

            var notification = new Notification
            {
                AccountUsername = session.StudentUsername,
                NotificationType = NotificationType.HomeworkAssigned,
                Message = $"You have new homework - '{homeworkAssignment.Name}' for course {session.Course.Name}.",
                NotificationTime = DateTime.UtcNow
            };
            _context.Notification.Add(notification);

            await _context.SaveChangesAsync();

            var dto = new HomeworkAssignmentDto
            {
                HomeworkAssignmentID = homeworkAssignment.HomeworkAssignmentID,
                SessionID = homeworkAssignment.SessionID,
                Name = homeworkAssignment.Name,
                Objective = homeworkAssignment.Objective
            };

            return CreatedAtAction("GetHomeworkAssignment", new { id = homeworkAssignment.HomeworkAssignmentID }, dto);
        }

        [HttpPost("{id}/file")]
        public async Task<IActionResult> PostHomeworkAssignmentFile(int id, IFormFile file)
        {
            var username = GetCurrentUsername();
            var account = await _context.Account.FindAsync(username);
            if (account == null)
                return Unauthorized(ErrorMessages.UserNotFound);
            if (account.IsTutor)
                return Forbid(ErrorMessages.NotAStudent);

            var homeworkAssignment = await _context.HomeworkAssignment
                .Include(x => x.Session)
                .ThenInclude(s => s.Course)
                .FirstOrDefaultAsync(x => x.HomeworkAssignmentID == id);

            if (homeworkAssignment == null)
                return NotFound("No such homework assignment");
            if (!homeworkAssignment.Session.StudentUsername.Equals(username))
                return Forbid("Attempted to upload solution for homework assigned to a different student");
            if (homeworkAssignment.SolutionFileName != null)
                return BadRequest("A solution has already been uploaded for this homework assignment");

            homeworkAssignment.SolutionFileName = await _fileService.SaveFileAsync(file);

            var notification = new Notification
            {
                AccountUsername = homeworkAssignment.Session.Course.TutorUsername,
                NotificationType = NotificationType.HomeworkSolutionUploaded,
                Message = $"Student {username} uploaded a solution for homework '{homeworkAssignment.Name}'.",
                NotificationTime = DateTime.UtcNow
            };
            _context.Notification.Add(notification);

            _context.Entry(homeworkAssignment).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch(DbUpdateConcurrencyException)
            {
                if (!await HomeworkAssignmentExistsAsync(id))
                    return NotFound("No such hoomework assignment");
                else
                    throw;
            }

            return NoContent();
        }

        [HttpGet("{id}/file")]
        public async Task<IActionResult> GetHomeworkAssignmentFile(int id)
        {
            var username = GetCurrentUsername();
            var account = await _context.Account.FindAsync(username);
            if (account == null)
                return Unauthorized(ErrorMessages.UserNotFound);

            var homeworkAssignment = await _context.HomeworkAssignment
                .Include(h => h.Session)
                .ThenInclude(s => s.Course)
                .FirstOrDefaultAsync(h => h.HomeworkAssignmentID == id);

            if (homeworkAssignment == null || string.IsNullOrEmpty(homeworkAssignment.SolutionFileName))
                return NotFound();

            if (account.IsTutor)
                if (homeworkAssignment.Session.Course.TutorUsername != username)
                    return Forbid("Attempted to download homework assigned by a different tutor");
            else
                if (homeworkAssignment.Session.StudentUsername != username)
                    return Forbid("Attempted to download homework assigned to a different student");

            var filePath = _fileService.GetFilePath(homeworkAssignment.SolutionFileName);
            if (!System.IO.File.Exists(filePath))
                return StatusCode(500, "Homework file no longer exists on the server");

            return PhysicalFile(filePath, "application/octet-stream", homeworkAssignment.SolutionFileName);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteHomeworkAssignment(int id)
        {
            var username = GetCurrentUsername();
            var account = await _context.Account.FindAsync(username);
            if (account == null)
                return Unauthorized(ErrorMessages.UserNotFound);

            if (!account.IsTutor)
                return Forbid(ErrorMessages.NotATutor);

            var homeworkAssignment = await _context.HomeworkAssignment
                .Include(h => h.Session)
                .ThenInclude(s => s.Course)
                .FirstOrDefaultAsync(h => h.HomeworkAssignmentID == id);

            if (homeworkAssignment == null)
                return NotFound();

            if (homeworkAssignment.Session.Course.TutorUsername != username)
                return Forbid("Attempted to delete homework assigned by a different tutor");

            _context.HomeworkAssignment.Remove(homeworkAssignment);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private async Task<bool> HomeworkAssignmentExistsAsync(int id)
        {
            return await _context.HomeworkAssignment.AnyAsync(e => e.HomeworkAssignmentID == id);
        }
    }
}
