using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TutorApp.API.Data;
using TutorApp.API.DTOs;
using TutorApp.API.Interfaces;
using TutorApp.API.Models;

namespace TutorApp.API.Controllers {
    [Route("api/[controller]")]
    [ApiController]
    public class TeachingMaterialController : BaseApiController
    {
        private readonly TutorDbContext _context;
        private readonly IFileService _fileService;

        private readonly string NoAccessOrNotFound = "The requested teaching material does not exist "
            + "or the current user does not have access to it";

        public TeachingMaterialController(TutorDbContext context, IFileService fileService)
        {
            _context = context;
            _fileService = fileService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TeachingMaterialDto>>> GetTeachingMaterials()
        {
            var username = GetCurrentUsername();
            var account = await _context.Account.FindAsync(username);
            if (account == null)
                return Unauthorized(ErrorMessages.UserNotFound);

            IEnumerable<TeachingMaterial> materials;

            if(account.IsTutor)
            {
                var allMaterials = await _context.TeachingMaterial
                    .Include(x => x.Course)
                    .ToListAsync();
                // Return materials for courses taught by this tutor
                materials = allMaterials.Where(x => x.Course.TutorUsername.Equals(username));
            } else {
                var allMaterials = await _context.TeachingMaterial
                    .Include(x => x.Course)
                    .ThenInclude(y => y.EnrolledStudents)
                    .ToListAsync();
                // Return materials for courses, for which there exists at least 1 enrollment
                // (Student_Course object) of the given student
                materials = allMaterials.Where(
                    x => x.Course.EnrolledStudents.Where(
                        y => y.StudentUsername.Equals(username)
                    ).Count()>=1);
            }

            return Ok(materials.Select(m => new TeachingMaterialDto
            {
                TeachingMaterialID = m.TeachingMaterialID,
                Name = m.Name,
                FileName = m.FileName,
                CourseID = m.CourseID
            }));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TeachingMaterialDto>> GetTeachingMaterial(int id)
        {
            var username = GetCurrentUsername();
            var account = await _context.Account.FindAsync(username);
            if (account == null)
                return Unauthorized(ErrorMessages.UserNotFound);

            var material = await RetrieveMaterialWithUserAsync(id, account);
            if (material == null)
                return NotFound(NoAccessOrNotFound);
            else
                return new TeachingMaterialDto
                {
                    TeachingMaterialID = material.TeachingMaterialID,
                    Name = material.Name,
                    FileName = material.FileName,
                    CourseID = material.CourseID
                };
           
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutTeachingMaterial(int id, TeachingMaterial teachingMaterial)
        {
            var username = GetCurrentUsername();
            if (id != teachingMaterial.TeachingMaterialID)
                return BadRequest("Teaching material ID is different in request path and request body");

            var account = await _context.Account.FindAsync(username);
            if (account == null)
                return Unauthorized(ErrorMessages.UserNotFound);
            var oldMaterial = await RetrieveMaterialWithUserAsync(id, account);

            if (oldMaterial == null)
                return Forbid(NoAccessOrNotFound);

            _context.Entry(teachingMaterial).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await TeachingMaterialExistsAsync(id))
                    return NotFound(NoAccessOrNotFound);
                else
                    throw;
            }

            return NoContent();
        }

        [HttpPost]
        public async Task<ActionResult<TeachingMaterialDto>> PostTeachingMaterial([FromForm] TeachingMaterialCreateDto teachingMaterialDto)
        {
            try {
                var username = GetCurrentUsername();
                var account = await _context.Account.FindAsync(username);
                if (account == null)
                    return Unauthorized(ErrorMessages.UserNotFound);
                if (!account.IsTutor)
                    return Forbid(ErrorMessages.NotATutor);

                var course = await _context.Course.FindAsync(teachingMaterialDto.CourseID);
                if (course == null)
                    return BadRequest("Invalid course ID");
                if (!course.TutorUsername.Equals(username))
                    return Forbid("Cannot add teaching material to courses taught by another tutor");

                var teachingMaterial = new TeachingMaterial {
                    Name = teachingMaterialDto.Name,
                    CourseID = teachingMaterialDto.CourseID
                };

                if (teachingMaterialDto.File != null)
                    teachingMaterial.FileName = await _fileService.SaveFileAsync(teachingMaterialDto.File);

                _context.TeachingMaterial.Add(teachingMaterial);
                await _context.SaveChangesAsync();

                var dto = new TeachingMaterialDto {
                    TeachingMaterialID = teachingMaterial.TeachingMaterialID,
                    Name = teachingMaterial.Name,
                    FileName = teachingMaterial.FileName,
                    CourseID = teachingMaterial.CourseID
                };

                return CreatedAtAction("GetTeachingMaterial", new { id = teachingMaterial.TeachingMaterialID }, dto);
            } catch(Exception ex) {
                return StatusCode(500, "Failed to upload teaching material due to server error: " + (ex.InnerException == null ? ex.Message : ex.InnerException.Message));
            }
        }

        [HttpGet("{id}/file")]
        public async Task<IActionResult> GetTeachingMaterialFile(int id)
        {
            var username = GetCurrentUsername();
            var account = await _context.Account.FindAsync(username);
            if (account == null)
                return Unauthorized(ErrorMessages.UserNotFound);

            var teachingMaterial = await RetrieveMaterialWithUserAsync(id, account);
            if (teachingMaterial == null || string.IsNullOrEmpty(teachingMaterial.FileName))
                return NotFound(NoAccessOrNotFound);

            var filePath = _fileService.GetFilePath(teachingMaterial.FileName);
            if (!System.IO.File.Exists(filePath))
                return StatusCode(500, "Teaching material not found in server file system");

            return PhysicalFile(filePath, "application/octet-stream", teachingMaterial.FileName);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTeachingMaterial(int id)
        {
            var username = GetCurrentUsername();
            var account = await _context.Account.FindAsync(username);
            if (account == null)
                return Unauthorized(ErrorMessages.UserNotFound);

            if (!account.IsTutor)
                return Forbid(ErrorMessages.NotATutor);

            var teachingMaterials = await _context.TeachingMaterial
                .Include(x => x.Course)
                .Where(x => x.Course.TutorUsername.Equals(username) && x.TeachingMaterialID == id)
                .ToListAsync();
            if (teachingMaterials.Count == 0)
                return NotFound(NoAccessOrNotFound);

            System.IO.File.Delete(teachingMaterials.First().FileName);
            _context.TeachingMaterial.Remove(teachingMaterials.First());
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private async Task<bool> TeachingMaterialExistsAsync(int id)
        {
            return await _context.TeachingMaterial.AnyAsync(e => e.TeachingMaterialID == id);
        }

        private async Task<TeachingMaterial> RetrieveMaterialWithUserAsync(int id, Account account) {
            if (account.IsTutor) {
                var materials = await _context.TeachingMaterial
                    .Include(x => x.Course)
                    // Return teaching materials with the provided ID if they were uploaded
                    // as part of a course taught by the current user
                    .Where(x => x.Course.TutorUsername.Equals(account.Username) && x.TeachingMaterialID == id)
                    .ToListAsync();
                if (materials.Count == 0)
                    return null;
                else
                    return materials.First();
            } else {
                var materials = await _context.TeachingMaterial
                    .Include(x => x.Course)
                    .ThenInclude(x => x.EnrolledStudents)
                    // Return teaching materials with the provided ID if they were uploaded
                    // as part of a course for which there is at least one enrolment
                    // (Student_Course object) related to the current user
                    .Where(x => x.Course.EnrolledStudents.Where(y => y.StudentUsername.Equals(account.Username)).Count() >= 1
                        && x.TeachingMaterialID == id)
                    .ToListAsync();
                if (materials.Count == 0)
                    return null;
                else
                    return materials.First();
            }
        }
    }
}
