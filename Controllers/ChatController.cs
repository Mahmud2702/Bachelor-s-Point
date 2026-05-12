using Bachelor_s_Point.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Bachelor_s_Point.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        private readonly IChatService _chatService;

        public ChatController(IChatService chatService)
        {
            _chatService = chatService;
        }

        // GET: /Chat — list of conversations
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            int userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var conversations = await _chatService.GetConversationsAsync(userId);
            return View(conversations);
        }

        // GET: /Chat/Conversation/{otherUserId}
        [HttpGet]
        public async Task<IActionResult> Conversation(int id)
        {
            int currentUserId = GetCurrentUserId();
            if (currentUserId == 0) return Unauthorized();

            if (id == currentUserId)
            {
                TempData["Error"] = "You cannot chat with yourself.";
                return RedirectToAction(nameof(Index));
            }

            var (messages, otherUser) = await _chatService.GetThreadAsync(currentUserId, id);
            if (otherUser == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.OtherUser = otherUser;
            ViewBag.CurrentUserId = currentUserId;
            return View(messages);
        }

        // POST: /Chat/Send
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(int receiverId, string content)
        {
            int currentUserId = GetCurrentUserId();
            if (currentUserId == 0) return Unauthorized();

            string result = await _chatService.SendMessageAsync(currentUserId, receiverId, content);
            if (result != "Success")
            {
                TempData["Error"] = result;
            }

            return RedirectToAction(nameof(Conversation), new { id = receiverId });
        }

        private int GetCurrentUserId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(idClaim, out int id) ? id : 0;
        }
    }
}
