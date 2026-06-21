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
        private readonly IPaymentService      _paymentService;
        private readonly IRoomService         _roomService;
        private readonly IUserService         _userService;
        private readonly ISSLCommerzService   _sslCommerz;
        private readonly IOptions<PaymentSettings>    _paySettings;
        private readonly IOptions<SSLCommerzSettings> _sslSettings;

        public PaymentController(
            IPaymentService      paymentService,
            IRoomService         roomService,
            IUserService         userService,
            ISSLCommerzService   sslCommerz,
            IOptions<PaymentSettings>    paySettings,
            IOptions<SSLCommerzSettings> sslSettings)
        {
            _paymentService = paymentService;
            _roomService    = roomService;
            _userService    = userService;
            _sslCommerz     = sslCommerz;
            _paySettings    = paySettings;
            _sslSettings    = sslSettings;
        }

        // ── REGISTRATION FEE PAGE ────────────────────────────────

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> RegistrationFee()
        {
            int userId = GetCurrentUserId();
            var user   = await _userService.GetUserByIdAsync(userId);
            if (user == null) return RedirectToAction("Login", "Auth");

            bool claimVerified = User.FindFirst("PaymentVerified")?.Value == "True";
            if (user.IsPaymentVerified && !claimVerified)
                return RedirectToAction("RefreshSession", "Auth");

            if (user.IsPaymentVerified)
            {
                TempData["Success"] = "Your account is already unlocked!";
                return RedirectToAction("Index", "Room");
            }

            ViewBag.PaySettings     = _paySettings.Value;
            ViewBag.ExistingPayment = await _paymentService.GetRegistrationPaymentAsync(userId);
            return View();
        }

        // ── PAY REGISTRATION FEE via SSLCommerz ──────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> PayRegistrationFee()
        {
            int userId = GetCurrentUserId();
            var user   = await _userService.GetUserByIdAsync(userId);
            if (user == null || user.IsPaymentVerified)
                return RedirectToAction(nameof(RegistrationFee));

            decimal amount  = _paySettings.Value.RegistrationFee;
            string  tranId  = $"BP-REG-{userId}-{DateTime.Now.Ticks}";
            string  baseUrl = _sslSettings.Value.BaseUrl.TrimEnd('/');

            // Create pending payment record first
            await _paymentService.SubmitRegistrationPaymentAsync(userId, tranId);

            string? gatewayUrl = await _sslCommerz.InitiatePaymentAsync(
                tranId, amount,
                successUrl:    $"{baseUrl}/Payment/SSLCommerzSuccess",
                failUrl:       $"{baseUrl}/Payment/SSLCommerzFail",
                cancelUrl:     $"{baseUrl}/Payment/SSLCommerzCancel",
                ipnUrl:        $"{baseUrl}/Payment/SSLCommerzIpn",
                customerName:  user.FullName  ?? user.UserName ?? "Customer",
                customerEmail: user.Email     ?? "customer@email.com",
                customerPhone: user.PhoneNumber ?? "01700000000",
                productName:   "Registration Fee - Bachelor's Point");

            if (gatewayUrl == null)
            {
                TempData["Error"] = "Payment gateway is currently unavailable. Please try again.";
                return RedirectToAction(nameof(RegistrationFee));
            }

            return Redirect(gatewayUrl);
        }

        // ── ROOM FEE PAGE ────────────────────────────────────────

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

        // ── PAY ROOM FEE via SSLCommerz ──────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> PayRoomFee(int roomId)
        {
            int userId = GetCurrentUserId();
            var room   = await _roomService.GetRoomByIdAsync(roomId);
            if (room == null) return NotFound();
            if (room.UserId != userId) return Forbid();

            int     pct    = _paySettings.Value.RoomFeePercent;
            decimal fee    = Math.Ceiling(room.Price * pct / 100.0m);
            string  tranId = $"BP-ROOM-{roomId}-{userId}-{DateTime.Now.Ticks}";
            string  baseUrl= _sslSettings.Value.BaseUrl.TrimEnd('/');

            var user = await _userService.GetUserByIdAsync(userId);

            // Create pending payment record first
            await _paymentService.SubmitRoomPaymentAsync(userId, roomId, tranId, fee);

            string? gatewayUrl = await _sslCommerz.InitiatePaymentAsync(
                tranId, fee,
                successUrl:    $"{baseUrl}/Payment/SSLCommerzSuccess",
                failUrl:       $"{baseUrl}/Payment/SSLCommerzFail",
                cancelUrl:     $"{baseUrl}/Payment/SSLCommerzCancel",
                ipnUrl:        $"{baseUrl}/Payment/SSLCommerzIpn",
                customerName:  user?.FullName  ?? user?.UserName ?? "Customer",
                customerEmail: user?.Email     ?? "customer@email.com",
                customerPhone: user?.PhoneNumber ?? "01700000000",
                productName:   $"Room Posting Fee - {room.Title}");

            if (gatewayUrl == null)
            {
                TempData["Error"] = "Payment gateway is currently unavailable. Please try again.";
                return RedirectToAction(nameof(RoomFee), new { roomId });
            }

            return Redirect(gatewayUrl);
        }

        // ── SSLCOMMERZ CALLBACKS ─────────────────────────────────

        // SSLCommerz POSTs here after successful payment
        [HttpPost]
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> SSLCommerzSuccess(IFormCollection form)
        {
            string? valId  = form["val_id"];
            string? tranId = form["tran_id"];

            if (string.IsNullOrEmpty(valId) || string.IsNullOrEmpty(tranId))
            {
                TempData["Error"] = "Payment validation failed.";
                return RedirectToAction("Index", "Home");
            }

            var (isValid, validatedTranId) = await _sslCommerz.ValidatePaymentAsync(valId);

            if (!isValid)
            {
                TempData["Error"] = "Payment could not be verified. Please contact support.";
                return RedirectToAction("Index", "Home");
            }

            string result = await _paymentService.VerifyPaymentByTranIdAsync(validatedTranId);

            if (result == "Success" || result == "Already verified")
            {
                if (validatedTranId.StartsWith("BP-REG-"))
                {
                    // Registration fee paid → refresh cookie so rooms unlock
                    if (User.Identity?.IsAuthenticated == true)
                        return RedirectToAction("RefreshSession", "Auth");

                    TempData["Success"] = "Payment successful! Please log in to unlock all rooms.";
                    return RedirectToAction("Login", "Auth");
                }
                else
                {
                    TempData["Success"] = "Room posting fee paid! Your room is now live.";
                    return RedirectToAction("MyListings", "Room");
                }
            }

            TempData["Error"] = "Payment processed but could not be recorded. Please contact support.";
            return RedirectToAction("Index", "Home");
        }

        // SSLCommerz POSTs here on payment failure
        [HttpPost]
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        public IActionResult SSLCommerzFail(IFormCollection form)
        {
            TempData["Error"] = "Payment failed. Please try again.";
            return RedirectAfterFailedAttempt(form["tran_id"]);
        }

        // SSLCommerz POSTs here when user cancels
        [HttpPost]
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        public IActionResult SSLCommerzCancel(IFormCollection form)
        {
            TempData["Error"] = "Payment cancelled.";
            return RedirectAfterFailedAttempt(form["tran_id"]);
        }

        private IActionResult RedirectAfterFailedAttempt(string? tranId)
        {
            if (!string.IsNullOrEmpty(tranId) && tranId.StartsWith("BP-ROOM-"))
            {
                var parts = tranId.Split('-');
                if (parts.Length > 2 && int.TryParse(parts[2], out int roomId))
                    return RedirectToAction(nameof(RoomFee), new { roomId });
                return RedirectToAction("MyListings", "Room");
            }
            return RedirectToAction(nameof(RegistrationFee));
        }

        // SSLCommerz IPN — background notification (backup verification)
        [HttpPost]
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> SSLCommerzIpn(IFormCollection form)
        {
            string? valId  = form["val_id"];
            string? tranId = form["tran_id"];

            if (!string.IsNullOrEmpty(valId) && !string.IsNullOrEmpty(tranId))
            {
                var (isValid, validatedTranId) = await _sslCommerz.ValidatePaymentAsync(valId);
                if (isValid)
                    await _paymentService.VerifyPaymentByTranIdAsync(validatedTranId);
            }

            return Ok();
        }

        // ── helper ──────────────────────────────────────────────
        private int GetCurrentUserId()
        {
            var val = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(val, out int id) ? id : 0;
        }
    }
}
