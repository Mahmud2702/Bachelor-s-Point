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
        private const int PageSize = 10;
        private readonly IRoomService _roomService;

        public RoomController(IRoomService roomService)
        {
            _roomService = roomService;
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

            // If the current user is the room owner, load all selections on this room
            // so they can see who's interested + the messages.
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
        public async Task<IActionResult> Create(CreateRoomDto dto)
        {
            if (!ModelState.IsValid) return View(dto);

            int currentUserId = GetCurrentUserId();
            bool isAdmin = User.IsInRole("Admin");

            string result = await _roomService.CreateRoomAsync(dto, currentUserId, autoApprove: isAdmin);

            if (result != "Success")
            {
                ModelState.AddModelError("", result);
                return View(dto);
            }

            TempData["Success"] = isAdmin
                ? "Room posted and published."
                : "Room submitted. It will appear on the home page once an admin approves it.";

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

            string result = await _roomService.DeleteRoomAsync(id, currentUserId, isAdmin);

            if (result != "Success") TempData["Error"] = result;
            else TempData["Success"] = "Room deleted";

            return isAdmin ? RedirectToAction(nameof(Pending)) : RedirectToAction("Profile", "Auth");
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
            => View(await _roomService.GetPendingApprovalAsync());

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

        private int GetCurrentUserId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(idClaim, out int id) ? id : 0;
        }
    }
}
