using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TutorApp.API.Data;
using TutorApp.API.DTOs;
using TutorApp.API.Models;

namespace TutorApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StudentCourseController : BaseApiController
    {
        private readonly TutorDbContext _context;

        public StudentCourseController(TutorDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<StudentCourseDto>>> GetCoursesByStudent()
        {
            var username = GetCurrentUsername();
            var account = await _context.Account.FindAsync(username);
            if (account == null)
                return Unauthorized(ErrorMessages.UserNotFound);
            if (account.IsTutor)
                return Forbid(ErrorMessages.NotAStudent);

            var studentCourses = await _context.StudentCourse.Where(x => x.StudentUsername.Equals(username)).ToListAsync();

            return Ok(studentCourses.Select(sc => new StudentCourseDto
            {
                StudentUsername = sc.StudentUsername,
                CourseID = sc.CourseID,
                Frequency = sc.Frequency,
                EndDate = sc.EndDate
            }));
        }

        [HttpGet("{courseId}")]
        public async Task<ActionResult<IEnumerable<StudentCourseDto>>> GetStudentsByCourse(int courseId)
        {
            var username = GetCurrentUsername();
            var account = await _context.Account.FindAsync(username);
            if (account == null)
                return Unauthorized(ErrorMessages.UserNotFound);
            if (!account.IsTutor)
                return Forbid(ErrorMessages.NotATutor);

            var course = await _context.Course.FindAsync(courseId);
            if (course == null)
                return NotFound("No such course");
            if (!course.TutorUsername.Equals(username))
                return Forbid("Cannot retrieve a list of students for a course not taught by the current user");

            var studentCourses = await _context.StudentCourse.Where(x => x.CourseID == courseId).ToListAsync();

            return Ok(studentCourses.Select(sc => new StudentCourseDto
            {
                StudentUsername = sc.StudentUsername,
                CourseID = sc.CourseID,
                Frequency = sc.Frequency,
                EndDate = sc.EndDate
            }));
        }

        [HttpGet("{courseId}/{studentUsername}")]
        public async Task<ActionResult<StudentCourseDto>> GetStudentCourse(string studentUsername, int courseId)
        {
            var username = GetCurrentUsername();
            var course = await _context.Course.FindAsync(courseId);
            if (course == null)
                return NotFound("No such course");

            if(!username.Equals(studentUsername)) {
                if (!course.TutorUsername.Equals(username))
                    return Forbid("To access this information, you must be either the student whom it is related to, "
                        + "or the tutor of the specified course");
            }

            var studentCourse = await _context.StudentCourse.FindAsync(studentUsername, courseId);

            if (studentCourse == null)
                return NotFound("The specified student is not assigned to the given course");

            return new StudentCourseDto
            {
                StudentUsername = studentCourse.StudentUsername,
                CourseID = studentCourse.CourseID,
                Frequency = studentCourse.Frequency,
                EndDate = studentCourse.EndDate
            };
        }

        [HttpPut("{courseId}/{studentUsername}")]
        public async Task<IActionResult> PutStudentCourse(string studentUsername, int courseId, Student_Course studentCourse)
        {
            var username = GetCurrentUsername();
            var account = await _context.Account.FindAsync(username);
            if (account == null)
                return Unauthorized(ErrorMessages.UserNotFound);
            if (!account.IsTutor)
                return Forbid(ErrorMessages.NotATutor);

            if (!studentUsername.Equals(studentCourse.StudentUsername))
                return BadRequest("Cannot change a student course assignment to point to a different student."
                    + " Please unassign the student from this course, and then assign the desired student to it.");

            if (courseId != studentCourse.CourseID)
                return BadRequest("Cannot change a student course assignment to point to a different course."
                    + " Please unassign the student from this course, and then assign the student to the desired course.");

            if (studentCourse.EndDate.CompareTo(DateTime.Now) < 0)
                return BadRequest("The new assignment end date cannot be in the past.");

            _context.Entry(studentCourse).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await StudentCourseExistsAsync(studentUsername, courseId))
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
        public async Task<ActionResult<StudentCourseDto>> PostStudentCourse(StudentCourseCreateDto studentCourseDto)
        {
            var username = GetCurrentUsername();
            var account = await _context.Account.FindAsync(username);
            if (account == null)
                return Unauthorized(ErrorMessages.UserNotFound);
            if (!account.IsTutor)
                return Forbid(ErrorMessages.NotATutor);

            var student = await _context.Account.FindAsync(studentCourseDto.StudentUsername);
            if (student == null)
                return BadRequest("No such student");

            var course = await _context.Course.FindAsync(studentCourseDto.CourseID);
            if (course == null)
                return BadRequest("No such course");

            if (!username.Equals(course.TutorUsername))
                return Forbid("Cannot assign students to other tutors' courses");

            if (studentCourseDto.EndDate.CompareTo(DateTime.Now) < 0)
                return BadRequest("The assignment end date cannot be in the past.");

            var studentCourse = new Student_Course
            {
                StudentUsername = studentCourseDto.StudentUsername,
                CourseID = studentCourseDto.CourseID,
                Frequency = studentCourseDto.Frequency,
                EndDate = studentCourseDto.EndDate
            };

            _context.StudentCourse.Add(studentCourse);
            
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (await StudentCourseExistsAsync(studentCourse.StudentUsername, studentCourse.CourseID))
                    return Conflict("This student is already assigned to this course.");
                else
                    throw;
            }

            var dto = new StudentCourseDto
            {
                StudentUsername = studentCourse.StudentUsername,
                CourseID = studentCourse.CourseID,
                Frequency = studentCourse.Frequency,
                EndDate = studentCourse.EndDate
            };

            return CreatedAtAction("GetStudentCourse", new { studentUsername = studentCourse.StudentUsername, courseId = studentCourse.CourseID }, dto);
        }

        [HttpDelete("{studentUsername}/{courseId}")]
        public async Task<IActionResult> DeleteStudentCourse(string studentUsername, int courseId)
        {
            var username = GetCurrentUsername();
            studentUsername = username;
            var studentCourse = await _context.StudentCourse.FindAsync(studentUsername, courseId);
            if (studentCourse == null)
            {
                return NotFound();
            }

            _context.StudentCourse.Remove(studentCourse);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private async Task<bool> StudentCourseExistsAsync(string studentUsername, int courseId)
        {
            return await _context.StudentCourse.AnyAsync(e => e.StudentUsername == studentUsername && e.CourseID == courseId);
        }
    }
}
