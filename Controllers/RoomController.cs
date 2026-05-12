using Bachelor_s_Point.Application.DTOs;
using Bachelor_s_Point.Application.Interfaces.Services;
using Bachelor_s_Point.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Bachelor_s_Point.Controllers
{
    public class RoomController : Controller
    {
        private const int PageSize = 9;
        private const long MaxImageBytes = 5 * 1024 * 1024; // 5MB per image
        private const int MaxImageCount = 10;               // up to 10 per room
        private static readonly string[] AllowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

        private readonly IRoomService _roomService;
        private readonly IWebHostEnvironment _env;

        public RoomController(IRoomService roomService, IWebHostEnvironment env)
        {
            _roomService = roomService;
            _env = env;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Index(string? searchText, int page = 1)
        {
            var paged = await _roomService.GetApprovedPagedAsync(searchText, page, PageSize);
            ViewBag.SearchText = searchText;
            return View(paged);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var room = await _roomService.GetRoomByIdAsync(id.Value);
            if (room == null) return NotFound();

            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                int currentUserId = GetCurrentUserId();
                if (currentUserId != 0 && room.UserId == currentUserId)
                {
                    ViewBag.RoomSelections = await _roomService.GetSelectionsForRoomAsync(room.Id);
                }
            }

            return View(room);
        }

        [HttpGet]
        [Authorize]
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        [RequestSizeLimit(60 * 1024 * 1024)]  // 10 images x 5MB + overhead
        public async Task<IActionResult> Create(CreateRoomDto dto, List<IFormFile>? roomImages)
        {
            if (!ModelState.IsValid) return View(dto);

            int currentUserId = GetCurrentUserId();
            bool isAdmin = User.IsInRole("Admin");

            var (result, roomId) = await _roomService.CreateRoomAsync(dto, currentUserId, autoApprove: isAdmin);
            if (result != "Success")
            {
                ModelState.AddModelError("", result);
                return View(dto);
            }

            // Save uploaded images
            if (roomImages != null && roomImages.Count > 0)
            {
                var valid = roomImages.Where(f => f != null && f.Length > 0).Take(MaxImageCount).ToList();

                string webRoot = !string.IsNullOrEmpty(_env.WebRootPath)
                    ? _env.WebRootPath
                    : Path.Combine(_env.ContentRootPath, "wwwroot");

                if (!Directory.Exists(webRoot)) Directory.CreateDirectory(webRoot);

                string uploadsFolder = Path.Combine(webRoot, "uploads", "rooms");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                int order = 0;
                foreach (var file in valid)
                {
                    string ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                    if (!AllowedExtensions.Contains(ext)) continue;
                    if (file.Length > MaxImageBytes) continue;
                    if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)) continue;

                    string fileName = $"room_{roomId}_{DateTime.UtcNow.Ticks}_{order}{ext}";
                    string savePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(savePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    string relativeUrl = $"/uploads/rooms/{fileName}";
                    await _roomService.AddRoomImageAsync(roomId, relativeUrl, isPrimary: order == 0, displayOrder: order);
                    order++;
                }
            }

            if (isAdmin)
                TempData["Success"] = "Room posted and published.";
            else
                TempData["Success"] = "Room submitted. It will appear on the home page once an admin approves it.";

            return RedirectToAction("Profile", "Auth");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> MyListings()
        {
            int currentUserId = GetCurrentUserId();
            var rooms = await _roomService.GetMyRoomsAsync(currentUserId);
            return View(rooms);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var room = await _roomService.GetRoomByIdAsync(id.Value);
            if (room == null) return NotFound();

            int currentUserId = GetCurrentUserId();
            bool isAdmin = User.IsInRole("Admin");
            if (!isAdmin && room.UserId != currentUserId) return Forbid();
            return View(room);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Edit(int id, Room room)
        {
            if (id != room.Id) return NotFound();
            ModelState.Remove("Owner");
            ModelState.Remove("Images");

            if (!ModelState.IsValid) return View(room);

            int currentUserId = GetCurrentUserId();
            bool isAdmin = User.IsInRole("Admin");

            string result = await _roomService.UpdateRoomAsync(room, currentUserId, isAdmin);
            if (result != "Success")
            {
                ModelState.AddModelError("", result);
                return View(room);
            }

            TempData["Success"] = "Room updated";
            return RedirectToAction("Profile", "Auth");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var room = await _roomService.GetRoomByIdAsync(id.Value);
            if (room == null) return NotFound();

            int currentUserId = GetCurrentUserId();
            bool isAdmin = User.IsInRole("Admin");
            if (!isAdmin && room.UserId != currentUserId) return Forbid();
            return View(room);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            int currentUserId = GetCurrentUserId();
            bool isAdmin = User.IsInRole("Admin");

            // Try to delete image files from disk before removing DB record
            var room = await _roomService.GetRoomByIdAsync(id);
            if (room != null && room.Images != null && (isAdmin || room.UserId == currentUserId))
            {
                string webRoot = !string.IsNullOrEmpty(_env.WebRootPath)
                    ? _env.WebRootPath
                    : Path.Combine(_env.ContentRootPath, "wwwroot");

                foreach (var img in room.Images)
                {
                    if (string.IsNullOrEmpty(img.ImagePath)) continue;
                    string relative = img.ImagePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                    string fullPath = Path.Combine(webRoot, relative);
                    if (System.IO.File.Exists(fullPath))
                    {
                        try { System.IO.File.Delete(fullPath); } catch { }
                    }
                }
            }

            string result = await _roomService.DeleteRoomAsync(id, currentUserId, isAdmin);

            if (result != "Success")
                TempData["Error"] = result;
            else
                TempData["Success"] = "Room deleted";

            if (isAdmin)
                return RedirectToAction(nameof(Pending));
            return RedirectToAction("Profile", "Auth");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Select(int? id)
        {
            if (id == null) return NotFound();

            if (User.IsInRole("Admin"))
            {
                TempData["Error"] = "Admin cannot select rooms.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var room = await _roomService.GetRoomByIdAsync(id.Value);
            if (room == null) return NotFound();

            int currentUserId = GetCurrentUserId();
            if (room.UserId == currentUserId)
            {
                TempData["Error"] = "You cannot select your own room.";
                return RedirectToAction(nameof(Details), new { id = room.Id });
            }

            ViewBag.SelectionDto = new SelectRoomDto { RoomId = room.Id };
            return View(room);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Select(SelectRoomDto dto)
        {
            if (User.IsInRole("Admin"))
            {
                TempData["Error"] = "Admin cannot select rooms.";
                return RedirectToAction(nameof(Details), new { id = dto.RoomId });
            }

            dto.SeekerUserId = GetCurrentUserId();
            string result = await _roomService.SelectRoomAsync(dto);

            if (result != "Success" && !result.StartsWith("Room selected"))
            {
                TempData["Error"] = result;
                return RedirectToAction(nameof(Details), new { id = dto.RoomId });
            }

            TempData["Success"] = result == "Success"
                ? "Room selected. The owner has been notified by email."
                : result;

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Pending()
        {
            var rooms = await _roomService.GetPendingApprovalAsync();
            return View(rooms);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Approve(int id)
        {
            string result = await _roomService.ApproveRoomAsync(id);
            if (result != "Success")
                TempData["Error"] = result;
            else
                TempData["Success"] = "Room approved and published.";
            return RedirectToAction(nameof(Pending));
        }

        private int GetCurrentUserId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(idClaim, out int id) ? id : 0;
        }
    }
}
