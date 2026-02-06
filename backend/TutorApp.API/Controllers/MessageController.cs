using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TutorApp.API.Data;
using TutorApp.API.DTOs;
using TutorApp.API.Interfaces;
using TutorApp.API.Models;

namespace TutorApp.API.Controllers {
    [Route("api/[controller]")]
    [ApiController]
    public class MessageController : BaseApiController
    {
        private readonly TutorDbContext _context;
        private readonly IFileService _fileService;

        public MessageController(TutorDbContext context, IFileService fileService)
        {
            _context = context;
            _fileService = fileService;
        }

        [HttpGet("/received")]
        public async Task<ActionResult<IEnumerable<MessageDto>>> GetReceivedMessages()
        {
            var username = GetCurrentUsername();
            var messages = await _context.Message.Where(x => x.RecipientUsername == username).ToListAsync();

            return Ok(messages.Select(m => new MessageDto
            {
                MessageID = m.MessageID,
                SenderUsername = m.SenderUsername,
                RecipientUsername = m.RecipientUsername,
                Topic = m.Topic,
                Body = m.Body,
                AttachmentFileName = m.AttachmentFileName,
                SentOn = m.SentOn
            }));
        }

        [HttpGet("/sent")]
        public async Task<ActionResult<IEnumerable<MessageDto>>> GetSentMessages() {
            var username = GetCurrentUsername();
            var messages = await _context.Message.Where(x => x.SenderUsername == username).ToListAsync();

            return Ok(messages.Select(m => new MessageDto
            {
                MessageID = m.MessageID,
                SenderUsername = m.SenderUsername,
                RecipientUsername = m.RecipientUsername,
                Topic = m.Topic,
                Body = m.Body,
                AttachmentFileName = m.AttachmentFileName,
                SentOn = m.SentOn
            }));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<MessageDto>> GetMessage(int id)
        {
            var username = GetCurrentUsername();
            var message = await _context.Message.FindAsync(id);

            if (message == null)
                return NotFound();

            if (!MessageBelongsTo(message, username))
                return Forbid("Requested message does not belong to the current user");

            return new MessageDto
            {
                MessageID = message.MessageID,
                SenderUsername = message.SenderUsername,
                RecipientUsername = message.RecipientUsername,
                Topic = message.Topic,
                Body = message.Body,
                AttachmentFileName = message.AttachmentFileName,
                SentOn = message.SentOn
            };
        }

        [HttpPost]
        public async Task<ActionResult<MessageDto>> PostMessage([FromForm] MessageCreateDto messageDto)
        {
            var username = GetCurrentUsername();

            if (username.Equals(messageDto.RecipientUsername))
                return BadRequest("Sending a message to yourself is not allowed");

            var message = new Message
            {
                SenderUsername = username,
                RecipientUsername = messageDto.RecipientUsername,
                Topic = messageDto.Topic,
                Body = messageDto.Body,
                SentOn = DateTime.UtcNow
            };

            if (messageDto.File != null)
                message.AttachmentFileName = await _fileService.SaveFileAsync(messageDto.File);

            _context.Message.Add(message);

            var notification = new Notification
            {
                AccountUsername = message.RecipientUsername,
                NotificationType = NotificationType.MessageReceived,
                Message = $"You received a new message from {username}.",
                NotificationTime = DateTime.UtcNow
            };
            _context.Notification.Add(notification);

            await _context.SaveChangesAsync();

            var dto = new MessageDto
            {
                MessageID = message.MessageID,
                SenderUsername = message.SenderUsername,
                RecipientUsername = message.RecipientUsername,
                Topic = message.Topic,
                Body = message.Body,
                AttachmentFileName = message.AttachmentFileName,
                SentOn = message.SentOn
            };

            return CreatedAtAction("GetMessage", new { id = message.MessageID }, dto);
        }

        [HttpGet("{id}/file")]
        public async Task<IActionResult> GetMessageFile(int id)
        {
            var username = GetCurrentUsername();
            var message = await _context.Message.FindAsync(id);
            if (message == null || string.IsNullOrEmpty(message.AttachmentFileName))
                return NotFound();

            if (!MessageBelongsTo(message, username))
                return Forbid("Requested attachment does not belong to current user");

            var filePath = _fileService.GetFilePath(message.AttachmentFileName);
            if (!System.IO.File.Exists(filePath))
                return StatusCode(500, "Attachment file no longer exists on server");

            return PhysicalFile(filePath, "application/octet-stream", message.AttachmentFileName);
        }
        
        private bool MessageBelongsTo(Message message, string username) {
            return message.SenderUsername.Equals(username) || message.RecipientUsername.Equals(username);
        }
    }
}
