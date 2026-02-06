using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TutorApp.API.Data;
using TutorApp.API.DTOs;
using TutorApp.API.Models;

namespace TutorApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CourseController : BaseApiController
    {
        private readonly TutorDbContext _context;

        public CourseController(TutorDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CourseDto>>> GetCourses()
        {
            var username = GetCurrentUsername();
            var account = await _context.Account.FindAsync(username);
            if (account == null)
                return Unauthorized(ErrorMessages.UserNotFound);

            if (!account.IsTutor)
                return Forbid(ErrorMessages.NotATutor);

            var courses = await _context.Course.ToListAsync();

            return Ok(courses.Select(c => new CourseDto
            {
                CourseID = c.CourseID,
                TutorUsername = c.TutorUsername,
                Name = c.Name,
                PricePerSession = c.PricePerSession,
                Description = c.Description
            }));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CourseDto>> GetCourse(int id)
        {
            var username = GetCurrentUsername();
            var course = await _context.Course.FindAsync(id);

            var account = await _context.Account.FindAsync(username);
            if (account == null)
                return Unauthorized(ErrorMessages.UserNotFound);
            if (course == null)
                    return NotFound();
            if (account.IsTutor)
            {
                if (!course.TutorUsername.Equals(username))
                    return Forbid("Cannot get other tutors' courses");
            } else {
                var enrollments = await _context.StudentCourse.Where(x => x.StudentUsername.Equals(username) && x.CourseID == id).CountAsync();
                if (enrollments == 0)
                    return Forbid("Current user is not enrolled in the requested course");
            }

            return new CourseDto
            {
                CourseID = course.CourseID,
                TutorUsername = course.TutorUsername,
                Name = course.Name,
                PricePerSession = course.PricePerSession,
                Description = course.Description
            };
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutCourse(int id, CourseDto courseDto)
        {
            var username = GetCurrentUsername();
            if (id != courseDto.CourseID)
                return BadRequest("Course ID in request path does not match course ID in request body");

            var existingCourse = await _context.Course.FindAsync(id);
            if (existingCourse == null)
                return BadRequest("No such course");
            if (!existingCourse.TutorUsername.Equals(username))
                return Forbid("Cannot edit a course belonging to another tutor");
            if (!existingCourse.TutorUsername.Equals(courseDto.TutorUsername))
                return Forbid("Cannot change the tutor assigned to a given course");

            existingCourse.Name = courseDto.Name;
            existingCourse.PricePerSession = courseDto.PricePerSession;
            existingCourse.Description = courseDto.Description;

            _context.Entry(existingCourse).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CourseExists(id))
                    return NotFound();
                else
                    throw;
            }

            return NoContent();
        }

        [HttpPost]
        public async Task<ActionResult<CourseDto>> PostCourse(CourseCreateDto createDto)
        {
            var username = GetCurrentUsername();
            var account = await _context.Account.FindAsync(username);
            if (account == null)
                return Unauthorized(ErrorMessages.UserNotFound);

            if (!account.IsTutor)
                return Forbid(ErrorMessages.NotATutor);

            if(createDto == null)
                return BadRequest();

            if (!createDto.TutorUsername.Equals(username))
                return Forbid("Creating a course for another tutor is not allowed");
            var course = new Course() {
                TutorUsername = createDto.TutorUsername,
                Name = createDto.Name,
                Description = createDto.Description,
                PricePerSession = createDto.PricePerSession
            };
            _context.Course.Add(course);
            await _context.SaveChangesAsync();

            var result = new CourseDto
            {
                CourseID = course.CourseID,
                TutorUsername = course.TutorUsername,
                Name = course.Name,
                PricePerSession = course.PricePerSession,
                Description = course.Description
            };

            return CreatedAtAction("GetCourse", new { id = result.CourseID }, result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            var username = GetCurrentUsername();
            var account = await _context.Account.FindAsync(username);
            if (account == null)
                return Unauthorized(ErrorMessages.UserNotFound);

            if (!account.IsTutor)
                return Forbid(ErrorMessages.NotATutor);

            var course = await _context.Course.FindAsync(id);
            if (course == null)
                return NotFound();

            if (!course.TutorUsername.Equals(username))
                return Forbid("Deleting another tutor's course is not allowed");

            _context.Course.Remove(course);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool CourseExists(int id)
        {
            return _context.Course.Any(e => e.CourseID == id);
        }
    }
}
