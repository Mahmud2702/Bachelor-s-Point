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
        private readonly IRoomService _roomService;

        public RoomController(IRoomService roomService)
        {
            _roomService = roomService;
        }

        // ---------------- Public browsing ----------------

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Index(string? searchText)
        {
            var rooms = string.IsNullOrWhiteSpace(searchText)
                ? await _roomService.GetAllAvailableRoomsAsync()
                : await _roomService.SearchAsync(searchText);

            ViewBag.SearchText = searchText;
            return View(rooms);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var room = await _roomService.GetRoomByIdAsync(id.Value);

            if (room == null) return NotFound();

            return View(room);
        }

        // ---------------- Owner management — any logged-in user ----------------

        [HttpGet]
        [Authorize]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Create(CreateRoomDto dto)
        {
            if (!ModelState.IsValid)
            {
                return View(dto);
            }

            int currentUserId = GetCurrentUserId();

            string result = await _roomService.CreateRoomAsync(dto, currentUserId);

            if (result != "Success")
            {
                ModelState.AddModelError("", result);
                return View(dto);
            }

            TempData["Success"] = "Room posted successfully";
            return RedirectToAction(nameof(MyListings));
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

            if (!isAdmin && room.UserId != currentUserId)
            {
                return Forbid();
            }

            return View(room);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Edit(int id, Room room)
        {
            if (id != room.Id) return NotFound();

            ModelState.Remove("Owner");

            if (!ModelState.IsValid)
            {
                return View(room);
            }

            int currentUserId = GetCurrentUserId();
            bool isAdmin = User.IsInRole("Admin");

            string result = await _roomService.UpdateRoomAsync(room, currentUserId, isAdmin);

            if (result != "Success")
            {
                ModelState.AddModelError("", result);
                return View(room);
            }

            TempData["Success"] = "Room updated";
            return RedirectToAction(nameof(MyListings));
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

            if (!isAdmin && room.UserId != currentUserId)
            {
                return Forbid();
            }

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

            if (result != "Success")
            {
                TempData["Error"] = result;
            }
            else
            {
                TempData["Success"] = "Room deleted";
            }

            return RedirectToAction(nameof(MyListings));
        }

        // ---------------- Select Room workflow — any logged-in user ----------------

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Select(int? id)
        {
            if (id == null) return NotFound();

            var room = await _roomService.GetRoomByIdAsync(id.Value);

            if (room == null) return NotFound();

            // Prevent users from selecting their own rooms in the UI
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

        private int GetCurrentUserId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(idClaim, out int id) ? id : 0;
        }
    }
}
