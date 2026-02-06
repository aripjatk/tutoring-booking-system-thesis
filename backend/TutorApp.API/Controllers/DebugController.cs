using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TutorApp.API.Controllers {
    [Route("api/[controller]")]
    [ApiController]
    public class DebugController : ControllerBase {
        // This requires a valid token, but does NO database work and NO complex serialization
        [Authorize]
        [HttpGet("test-auth")]
        public IActionResult TestAuth() {
            return Ok(new { message = "Authentication is working properly!" });
        }
    }
}