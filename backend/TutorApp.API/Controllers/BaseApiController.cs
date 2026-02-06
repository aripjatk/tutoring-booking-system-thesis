using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace TutorApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BaseApiController : ControllerBase
    {
        protected string GetCurrentUsername()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new InvalidOperationException("User is not authenticated or NameIdentifier claim is missing");
        }
    }
}
