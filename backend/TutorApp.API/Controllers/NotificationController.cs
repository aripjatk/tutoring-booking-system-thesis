using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TutorApp.API.Data;
using TutorApp.API.DTOs;
using TutorApp.API.Models;

namespace TutorApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController : BaseApiController
    {
        private readonly TutorDbContext _context;

        public NotificationController(TutorDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<NotificationDto>>> GetNotifications()
        {
            var username = GetCurrentUsername();
            var notifications = await _context.Notification
                .Where(n => n.AccountUsername == username)
                .OrderByDescending(n => n.NotificationTime)
                .ToListAsync();

            return Ok(notifications.Select(n => new NotificationDto
            {
                NotificationID = n.NotificationID,
                AccountUsername = n.AccountUsername,
                NotificationType = n.NotificationType,
                Message = n.Message,
                NotificationTime = n.NotificationTime
            }));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<NotificationDto>> GetNotification(int id)
        {
            var username = GetCurrentUsername();
            var notification = await _context.Notification.FindAsync(id);

            if (notification == null)
            {
                return NotFound();
            }

            if (notification.AccountUsername != username)
            {
                return Forbid("Cannot view notifications of another user.");
            }

            return new NotificationDto
            {
                NotificationID = notification.NotificationID,
                AccountUsername = notification.AccountUsername,
                NotificationType = notification.NotificationType,
                Message = notification.Message,
                NotificationTime = notification.NotificationTime
            };
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNotification(int id)
        {
            var username = GetCurrentUsername();
            var notification = await _context.Notification.FindAsync(id);
            if (notification == null)
            {
                return NotFound();
            }

            if (notification.AccountUsername != username)
            {
                return Forbid("Cannot delete notifications of another user.");
            }

            _context.Notification.Remove(notification);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
