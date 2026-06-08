using Bachelor_s_Point.Application.Interfaces.Services;
using Bachelor_s_Point.Infrastructure.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace Bachelor_s_Point.Controllers
{
    public class PaymentController : Controller
    {
        private readonly IPaymentService _paymentService;
        private readonly IRoomService    _roomService;
        private readonly IUserService    _userService;
        private readonly IOptions<PaymentSettings> _paySettings;

        public PaymentController(
            IPaymentService paymentService,
            IRoomService    roomService,
            IUserService    userService,
            IOptions<PaymentSettings> paySettings)
        {
            _paymentService = paymentService;
            _roomService    = roomService;
            _userService    = userService;
            _paySettings    = paySettings;
        }

        // ── REGISTRATION FEE (seeker unlocks blurred rooms) ─────────────

        // GET /Payment/RegistrationFee
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> RegistrationFee()
        {
            int userId = GetCurrentUserId();
            var user   = await _userService.GetUserByIdAsync(userId);
            if (user == null) return RedirectToAction("Login", "Auth");

            // DB says verified but cookie claim is stale → refresh cookie then show rooms
            bool claimVerified = User.FindFirst("PaymentVerified")?.Value == "True";
            if (user.IsPaymentVerified && !claimVerified)
                return RedirectToAction("RefreshSession", "Auth");

            if (user.IsPaymentVerified)
            {
                TempData["Success"] = "Your account is already unlocked. Browse all rooms!";
                return RedirectToAction("Index", "Room");
            }

            ViewBag.PaySettings     = _paySettings.Value;
            ViewBag.ExistingPayment = await _paymentService.GetRegistrationPaymentAsync(userId);
            return View();
        }

        // POST /Payment/RegistrationFee
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> RegistrationFee(string transactionId)
        {
            int    userId = GetCurrentUserId();
            string result = await _paymentService.SubmitRegistrationPaymentAsync(userId, transactionId);

            if (result == "Success")
                TempData["Success"] = "Transaction ID submitted! Admin will verify and unlock your account shortly.";
            else
                TempData["Error"] = result;

            return RedirectToAction(nameof(RegistrationFee));
        }

        // ── ROOM POSTING FEE (owner pays 20% of rent per room) ──────────

        // GET /Payment/RoomFee?roomId=X
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> RoomFee(int roomId)
        {
            int userId = GetCurrentUserId();
            var room   = await _roomService.GetRoomByIdAsync(roomId);
            if (room == null) return NotFound();
            if (room.UserId != userId) return Forbid();

            int     pct = _paySettings.Value.RoomFeePercent;
            decimal fee = Math.Ceiling(room.Price * pct / 100.0m);

            ViewBag.Room            = room;
            ViewBag.FeeAmount       = fee;
            ViewBag.FeePercent      = pct;
            ViewBag.PaySettings     = _paySettings.Value;
            ViewBag.ExistingPayment = await _paymentService.GetRoomPaymentAsync(roomId);
            return View();
        }

        // POST /Payment/RoomFee
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> RoomFee(int roomId, string transactionId)
        {
            int userId = GetCurrentUserId();
            var room   = await _roomService.GetRoomByIdAsync(roomId);
            if (room == null) return NotFound();
            if (room.UserId != userId) return Forbid();

            int     pct = _paySettings.Value.RoomFeePercent;
            decimal fee = Math.Ceiling(room.Price * pct / 100.0m);

            string result = await _paymentService.SubmitRoomPaymentAsync(userId, roomId, transactionId, fee);

            if (result == "Success")
                TempData["Success"] = "Payment TrxID submitted! Admin will verify and publish your room shortly.";
            else
                TempData["Error"] = result;

            return RedirectToAction(nameof(RoomFee), new { roomId });
        }

        // ── ADMIN PANEL ─────────────────────────────────────────────────

        // GET /Payment/AdminPayments
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminPayments()
        {
            var payments = await _paymentService.GetAllPendingAsync();
            return View(payments);
        }

        // POST /Payment/Verify/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Verify(int id)
        {
            string result = await _paymentService.VerifyPaymentAsync(id);
            if (result == "Success")
                TempData["Success"] = "Payment verified. Account/Room activated.";
            else
                TempData["Error"] = result;

            return RedirectToAction(nameof(AdminPayments));
        }

        // POST /Payment/Reject/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Reject(int id, string? note)
        {
            string result = await _paymentService.RejectPaymentAsync(id, note);
            if (result == "Success")
                TempData["Success"] = "Payment rejected.";
            else
                TempData["Error"] = result;

            return RedirectToAction(nameof(AdminPayments));
        }

        // ── helper ──────────────────────────────────────────────────────
        private int GetCurrentUserId()
        {
            var val = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(val, out int id) ? id : 0;
        }
    }
}
