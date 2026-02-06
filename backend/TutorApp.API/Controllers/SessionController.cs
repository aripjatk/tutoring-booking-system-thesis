using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TutorApp.API.Data;
using TutorApp.API.DTOs;
using TutorApp.API.Models;

namespace TutorApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SessionController : BaseApiController
    {
        private readonly TutorDbContext _context;

        public SessionController(TutorDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<SessionDto>>> GetSessions()
        {
            var username = GetCurrentUsername();
            var account = await _context.Account.FindAsync(username);
            if (account == null)
                return Unauthorized(ErrorMessages.UserNotFound);

            List<Session> sessions;

            if(account.IsTutor)
            {
                sessions = await _context.Session
                    .Include(x => x.Course)
                    .Where(x => x.Course.TutorUsername.Equals(username))
                    .ToListAsync();
            } else {
                sessions = await _context.Session
                    .Where(x => x.StudentUsername.Equals(username))
                    .ToListAsync();
            }

            return Ok(sessions.Select(s => new SessionDto
            {
                SessionID = s.SessionID,
                StudentUsername = s.StudentUsername,
                CourseID = s.CourseID,
                SessionDateTime = s.SessionDateTime,
                IsPaidFor = s.IsPaidFor,
                ConfirmationStatus = s.ConfirmationStatus
            }));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<SessionDto>> GetSession(int id)
        {
            var username = GetCurrentUsername();
            var account = await _context.Account.FindAsync(username);
            if (account == null)
                return Unauthorized(ErrorMessages.UserNotFound);

            var session = await _context.Session.FindAsync(id);
            if (session == null)
                return NotFound();

            if(account.IsTutor)
            {
                var course = await _context.Course.FindAsync(session.CourseID);
                if (course == null)
                    return StatusCode(500, "Session has an invalid CourseID");
                if (!course.TutorUsername.Equals(username))
                    return Forbid("This session is part of a course not taught by the current user");

            } else {
                if (!session.StudentUsername.Equals(username))
                    return Forbid("The current user does not have access to this session");
            }

            var sessionWithAssignments = await _context.Session.Include(x => x.HomeworkAssignments)
                .SingleOrDefaultAsync(x => x.SessionID == id);

            if (sessionWithAssignments == null)
                return StatusCode(500, "Failed to retrieve list of homework assignments for session");

            var homeworkAssignmentDtos = sessionWithAssignments.HomeworkAssignments
                .Select(x => new HomeworkAssignmentDto {
                    HomeworkAssignmentID = x.HomeworkAssignmentID,
                    SessionID = x.SessionID,
                    Name = x.Name,
                    Objective = x.Objective,
                    HasSolutionFile = x.SolutionFileName != null,
                    SolutionFeedback = x.SolutionFeedback
                });

            return new SessionDto
            {
                SessionID = session.SessionID,
                StudentUsername = session.StudentUsername,
                CourseID = session.CourseID,
                SessionDateTime = session.SessionDateTime,
                IsPaidFor = session.IsPaidFor,
                ConfirmationStatus = session.ConfirmationStatus,
                HomeworkAssignments = homeworkAssignmentDtos
            };
        }

        private async Task<IActionResult> RespondToSession(int id, bool confirm)
        {
            var username = GetCurrentUsername();
            var account = await _context.Account.FindAsync(username);
            if (account == null)
                return Unauthorized(ErrorMessages.UserNotFound);
            if (account.IsTutor)
                return Forbid(ErrorMessages.NotAStudent);

            var session = await _context.Session.FindAsync(id);
            if (session == null)
                return NotFound("No such session");
            if (!session.StudentUsername.Equals(username))
                return Forbid("Cannot reject sessions for other students");
            if (session.ConfirmationStatus != ConfirmationStatus.Unknown)
                return BadRequest("Session has already been confirmed/rejected");

            if (confirm)
                session.ConfirmationStatus = ConfirmationStatus.Yes;
            else
                session.ConfirmationStatus = ConfirmationStatus.No;

            var course = await _context.Course.FindAsync(session.CourseID);

            if (course == null)
                return StatusCode(500, "Session is part of a course that doesn't exist (DB corrupted?)");

            var notification = new Notification
            {
                AccountUsername = course.TutorUsername,
                NotificationType = confirm ? NotificationType.SessionAccepted : NotificationType.SessionRejected,
                Message = $"Student {username} {(confirm ? "accepted" : "rejected")} session for course {course.Name}.",
                NotificationTime = DateTime.UtcNow
            };
            _context.Notification.Add(notification);

            _context.Entry(session).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await SessionExistsAsync(id))
                    return NotFound();
                else
                    throw;
            }
            return NoContent();
        }

        [HttpGet("{id}/accept")]
        public async Task<IActionResult> AcceptSession(int id)
        {
            return await RespondToSession(id, true);
        }

        [HttpGet("{id}/reject")]
        public async Task<IActionResult> RejectSession(int id)
        {
            return await RespondToSession(id, false);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutSession(int id, SessionDto session)
        {
            var username = GetCurrentUsername();
            var account = await _context.Account.FindAsync(username);
            if (account == null)
                return Unauthorized(ErrorMessages.UserNotFound);
            if (!account.IsTutor)
                return Forbid(ErrorMessages.NotATutor);

            if (id != session.SessionID)
                return BadRequest("Session ID in request path does not match request body");

            var originalSession = await _context.Session.FindAsync(id);
            if (originalSession == null)
                return BadRequest("No such session");

            if (originalSession.CourseID != session.CourseID
                || originalSession.StudentUsername != session.StudentUsername)
                return Forbid("Cannot change a session's course or student");
            if (originalSession.ConfirmationStatus != session.ConfirmationStatus)
                return Forbid(ErrorMessages.NotAStudent);

            var course = await _context.Course.FindAsync(session.CourseID);
            if (course == null)
                return StatusCode(500, "Session has an invalid CourseID (DB corrupted?)");
            if (!course.TutorUsername.Equals(username))
                return Forbid("Cannot edit other tutors' sessions");

            originalSession.IsPaidFor = session.IsPaidFor;
            if(originalSession.SessionDateTime != session.SessionDateTime) {
                originalSession.ConfirmationStatus = ConfirmationStatus.Unknown;
                originalSession.SessionDateTime = session.SessionDateTime;
            }

            _context.Entry(originalSession).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            } catch(Exception) {
                if (!await SessionExistsAsync(id))
                    return BadRequest("No such session");
                else throw;
            }
            return NoContent();
        }

        [HttpPost]
        public async Task<ActionResult<SessionDto>> PostSession(SessionCreateDto sessionDto)
        {
            var username = GetCurrentUsername();
            var account = await _context.Account.FindAsync(username);
            if (account == null)
                return Unauthorized(ErrorMessages.UserNotFound);
            if (!account.IsTutor)
                return Forbid(ErrorMessages.NotATutor);

            if (sessionDto.SessionDateTime.CompareTo(DateTime.UtcNow) < 0)
                return BadRequest("Cannot create a session with a time that is in the past");

            var course = await _context.Course.FindAsync(sessionDto.CourseID);
            if (course == null)
                return BadRequest("Invalid CourseID");
            if (!course.TutorUsername.Equals(username))
                return Forbid("Cannot add a session to a course not owned by the current user");

            var session = new Session
            {
                StudentUsername = sessionDto.StudentUsername,
                CourseID = sessionDto.CourseID,
                SessionDateTime = sessionDto.SessionDateTime,
                IsPaidFor = sessionDto.IsPaidFor,
                ConfirmationStatus = ConfirmationStatus.Unknown
            };

            _context.Session.Add(session);

            var notification = new Notification
            {
                AccountUsername = session.StudentUsername,
                NotificationType = NotificationType.SessionCreated,
                Message = $"Tutor {username} created a new session for course {course.Name}.",
                NotificationTime = DateTime.UtcNow
            };
            _context.Notification.Add(notification);

            await _context.SaveChangesAsync();

            var resultDto = new SessionDto
            {
                SessionID = session.SessionID,
                StudentUsername = session.StudentUsername,
                CourseID = session.CourseID,
                SessionDateTime = session.SessionDateTime,
                IsPaidFor = session.IsPaidFor,
                ConfirmationStatus = session.ConfirmationStatus
            };

            return CreatedAtAction("GetSession", new { id = session.SessionID }, resultDto);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSession(int id)
        {
            var username = GetCurrentUsername();
            var account = await _context.Account.FindAsync(username);
            if (account == null)
                return Unauthorized(ErrorMessages.UserNotFound);
            if (!account.IsTutor)
                return Forbid(ErrorMessages.NotATutor);

            var session = await _context.Session.FindAsync(id);
            if (session == null)
                return NotFound();

            var course = await _context.Course.FindAsync(session.CourseID);
            if (course == null)
                return StatusCode(500, "Session has an invalid CourseID");
            if (!course.TutorUsername.Equals(username))
                return Forbid("This session is part of a course not taught by the current user");

            _context.Session.Remove(session);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private async Task<bool> SessionExistsAsync(int id) {
            return await _context.Session.AnyAsync(s => s.SessionID == id);
        }
    }
}
