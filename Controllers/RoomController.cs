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
        private const int  PageSize      = 9;
        private const long MaxImageBytes = 5 * 1024 * 1024;
        private const int  MaxImageCount = 10;
        private static readonly string[] AllowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

        private readonly IRoomService        _roomService;
        private readonly IKycService         _kycService;
        private readonly IWebHostEnvironment _env;

        public RoomController(IRoomService roomService, IKycService kycService, IWebHostEnvironment env)
        {
            _roomService = roomService;
            _kycService  = kycService;
            _env         = env;
        }

        private async Task<IActionResult?> RequireKycAsync(string actionLabel)
        {
            if (User.IsInRole("Admin")) return null;
            int uid = GetCurrentUserId();
            if (uid == 0) return RedirectToAction("Login", "Auth");
            bool verified = await _kycService.IsUserVerifiedAsync(uid);
            if (verified) return null;
            TempData["Error"] = $"You must complete identity (KYC) verification before you can {actionLabel}.";
            return RedirectToAction("Status", "Kyc");
        }

        // ── INDEX / BROWSE ───────────────────────────────────────

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Index(
            string? searchText, string? division, string? district,
            int? minPrice, int? maxPrice,
            bool hasWifi = false, bool hasMeal = false, bool hasMaid = false,
            bool availableOnly = false, string? sortBy = null, int page = 1)
        {
            var filter = new RoomFilterDto
            {
                SearchText    = searchText,
                Division      = division,
                District      = district,
                MinPrice      = minPrice,
                MaxPrice      = maxPrice,
                HasWifi       = hasWifi,
                HasMeal       = hasMeal,
                HasMaid       = hasMaid,
                AvailableOnly = availableOnly,
                SortBy        = sortBy,
                Page          = page
            };

            var paged = await _roomService.GetFilteredPagedAsync(filter, PageSize);
            ViewBag.Filter     = filter;
            ViewBag.SearchText = searchText;
            return View(paged);
        }

        // ── DETAILS ─────────────────────────────────────────────

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var room = await _roomService.GetRoomByIdAsync(id.Value);
            if (room == null) return NotFound();

            if (User.Identity?.IsAuthenticated == true)
            {
                int uid = GetCurrentUserId();
                if (uid != 0 && room.UserId == uid)
                    ViewBag.RoomSelections = await _roomService.GetSelectionsForRoomAsync(room.Id);
            }
            return View(room);
        }

        // ── CREATE ───────────────────────────────────────────────

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Create()
        {
            var gate = await RequireKycAsync("post a room");
            if (gate != null) return gate;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        [RequestSizeLimit(60 * 1024 * 1024)]
        public async Task<IActionResult> Create(CreateRoomDto dto, List<IFormFile>? roomImages)
        {
            var gate = await RequireKycAsync("post a room");
            if (gate != null) return gate;

            if (!ModelState.IsValid) return View(dto);

            int  currentUserId = GetCurrentUserId();
            bool isAdmin       = User.IsInRole("Admin");

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
                string webRoot      = !string.IsNullOrEmpty(_env.WebRootPath) ? _env.WebRootPath : Path.Combine(_env.ContentRootPath, "wwwroot");
                string uploadFolder = Path.Combine(webRoot, "uploads", "rooms");
                Directory.CreateDirectory(uploadFolder);

                int order = 0;
                foreach (var file in valid)
                {
                    string ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                    if (!AllowedExtensions.Contains(ext)) continue;
                    if (file.Length > MaxImageBytes) continue;
                    if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)) continue;

                    string fileName = $"room_{roomId}_{DateTime.UtcNow.Ticks}_{order}{ext}";
                    string savePath = Path.Combine(uploadFolder, fileName);
                    using (var stream = new FileStream(savePath, FileMode.Create))
                        await file.CopyToAsync(stream);

                    await _roomService.AddRoomImageAsync(roomId, $"/uploads/rooms/{fileName}", isPrimary: order == 0, displayOrder: order);
                    order++;
                }
            }

            // Admin posts auto-approved — skip payment
            if (isAdmin)
            {
                TempData["Success"] = "Room posted and published.";
                return RedirectToAction("Profile", "Auth");
            }

            // Regular owner → must pay 20% posting fee before room is published
            return RedirectToAction("RoomFee", "Payment", new { roomId });
        }

        // ── MY LISTINGS ─────────────────────────────────────────

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> MyListings()
        {
            int uid = GetCurrentUserId();
            return View(await _roomService.GetMyRoomsAsync(uid));
        }

        // ── EDIT ────────────────────────────────────────────────

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var room = await _roomService.GetRoomByIdAsync(id.Value);
            if (room == null) return NotFound();

            int  uid     = GetCurrentUserId();
            bool isAdmin = User.IsInRole("Admin");
            if (!isAdmin && room.UserId != uid) return Forbid();
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

            int  uid     = GetCurrentUserId();
            bool isAdmin = User.IsInRole("Admin");

            string result = await _roomService.UpdateRoomAsync(room, uid, isAdmin);
            if (result != "Success")
            {
                ModelState.AddModelError("", result);
                return View(room);
            }

            TempData["Success"] = "Room updated";
            return RedirectToAction("Profile", "Auth");
        }

        // ── DELETE ───────────────────────────────────────────────

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var room = await _roomService.GetRoomByIdAsync(id.Value);
            if (room == null) return NotFound();

            int  uid     = GetCurrentUserId();
            bool isAdmin = User.IsInRole("Admin");
            if (!isAdmin && room.UserId != uid) return Forbid();
            return View(room);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            int  uid     = GetCurrentUserId();
            bool isAdmin = User.IsInRole("Admin");

            var room = await _roomService.GetRoomByIdAsync(id);
            if (room != null && room.Images != null && (isAdmin || room.UserId == uid))
            {
                string webRoot = !string.IsNullOrEmpty(_env.WebRootPath) ? _env.WebRootPath : Path.Combine(_env.ContentRootPath, "wwwroot");
                foreach (var img in room.Images)
                {
                    if (string.IsNullOrEmpty(img.ImagePath)) continue;
                    string full = Path.Combine(webRoot, img.ImagePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                    if (System.IO.File.Exists(full)) { try { System.IO.File.Delete(full); } catch { } }
                }
            }

            string result = await _roomService.DeleteRoomAsync(id, uid, isAdmin);
            if (result != "Success") TempData["Error"] = result;
            else TempData["Success"] = "Room deleted";

            return isAdmin
                ? RedirectToAction(nameof(Pending))
                : RedirectToAction("Profile", "Auth");
        }

        // ── SELECT ───────────────────────────────────────────────

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Select(int? id)
        {
            if (id == null) return NotFound();

            var gate = await RequireKycAsync("book a room");
            if (gate != null) return gate;

            if (User.IsInRole("Admin"))
            {
                TempData["Error"] = "Admin cannot select rooms.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var room = await _roomService.GetRoomByIdAsync(id.Value);
            if (room == null) return NotFound();

            int uid = GetCurrentUserId();
            if (room.UserId == uid)
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
            var gate = await RequireKycAsync("book a room");
            if (gate != null) return gate;

            if (User.IsInRole("Admin"))
            {
                TempData["Error"] = "Admin cannot select rooms.";
                return RedirectToAction(nameof(Details), new { id = dto.RoomId });
            }

            dto.SeekerUserId = GetCurrentUserId();
            string result    = await _roomService.SelectRoomAsync(dto);

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

        // ── ADMIN: PENDING APPROVAL ──────────────────────────────

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
            if (result != "Success") TempData["Error"] = result;
            else TempData["Success"] = "Room approved and published.";
            return RedirectToAction(nameof(Pending));
        }

        // ── HELPER ──────────────────────────────────────────────

        private int GetCurrentUserId()
        {
            var val = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(val, out int id) ? id : 0;
        }
    }
}
