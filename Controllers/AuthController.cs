using Bachelor_s_Point.Application.DTOs;
using Bachelor_s_Point.Application.Interfaces.Services;
using Bachelor_s_Point.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Bachelor_s_Point.Controllers
{
    public class AuthController : Controller
    {
        private readonly IAuthService    _authService;
        private readonly IRoleService    _roleService;
        private readonly IUserService    _userService;
        private readonly IRoomService    _roomService;
        private readonly IWebHostEnvironment _env;

        private static readonly string[] AllowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private const long MaxUploadBytes = 5 * 1024 * 1024;

        public AuthController(
            IAuthService authService,
            IRoleService roleService,
            IUserService userService,
            IRoomService roomService,
            IWebHostEnvironment env)
        {
            _authService = authService;
            _roleService = roleService;
            _userService = userService;
            _roomService = roomService;
            _env         = env;
        }

        // ── ROLE SELECT ─────────────────────────────────────────

        [HttpGet]
        public IActionResult RoleSelect()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");
            return View();
        }

        // ── REGISTER ────────────────────────────────────────────

        [HttpGet]
        public IActionResult Register(string? @as = null)
        {
            if (string.IsNullOrEmpty(@as)) @as = "user";
            ViewBag.RegisterType = @as;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterDto dto, string? @as = null)
        {
            if (string.IsNullOrEmpty(@as)) @as = "user";
            ViewBag.RegisterType = @as;

            var roles = await _roleService.GetAllRolesAsync();
            string targetRoleName = @as == "admin" ? "Admin" : "User";
            var targetRole = roles.FirstOrDefault(r => r.RoleName == targetRoleName);

            if (targetRole == null)
            {
                ModelState.AddModelError("", $"The {targetRoleName} role is missing. Run the migration first.");
                return View(dto);
            }

            dto.RoleId = targetRole.Id;
            ModelState.Remove(nameof(dto.RoleId));

            if (!ModelState.IsValid) return View(dto);

            string result = await _authService.StartRegistrationAsync(dto);
            if (result != "Success")
            {
                ModelState.AddModelError("", result);
                return View(dto);
            }

            TempData["Success"] = $"We've sent a 6-digit code to {dto.Email}. Enter it below.";
            return RedirectToAction(nameof(VerifyOtp), new { email = dto.Email });
        }

        // ── VERIFY OTP ──────────────────────────────────────────

        [HttpGet]
        public IActionResult VerifyOtp(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return RedirectToAction(nameof(Register));

            return View(new VerifyOtpDto { Email = email });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyOtp(VerifyOtpDto dto)
        {
            if (!ModelState.IsValid) return View(dto);

            var (result, newUser) = await _authService.VerifyOtpAndCreateUserAsync(dto.Email!, dto.Otp!);
            if (result != "Success")
            {
                ModelState.AddModelError("", result);
                ViewBag.AllowResend = result.Contains("expired", StringComparison.OrdinalIgnoreCase);
                return View(dto);
            }

            if (newUser != null)
            {
                await SignInUserAsync(newUser, rememberMe: false);

                // Admin is pre-verified → go straight to home
                if (newUser.IsPaymentVerified)
                {
                    TempData["Success"] = $"Welcome, {newUser.UserName}! Your account is ready.";
                    return RedirectToAction("Index", "Home");
                }
            }

            // Regular user → prompt them to pay (rooms will be blurred until then)
            return RedirectToAction("RegistrationFee", "Payment");
        }

        // ── RESEND OTP ──────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendOtp(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["Error"] = "Email is required to resend the code.";
                return RedirectToAction(nameof(Register));
            }

            string result = await _authService.ResendOtpAsync(email);
            if (result != "Success") TempData["Error"] = result;
            else TempData["Success"] = $"A new code has been sent to {email}.";

            return RedirectToAction(nameof(VerifyOtp), new { email });
        }

        // ── LOGIN ────────────────────────────────────────────────

        [HttpGet]
        public IActionResult Login(string? @as = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");
            ViewBag.LoginType = @as;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginDto dto, string? @as = null)
        {
            ViewBag.LoginType = @as;
            if (!ModelState.IsValid) return View(dto);

            var user = await _authService.LoginAsync(dto);
            if (user == null)
            {
                ModelState.AddModelError("", "Invalid email or password");
                return View(dto);
            }

            string? roleName = user.Role?.RoleName;

            if (@as == "admin" && roleName != "Admin")
            {
                ModelState.AddModelError("", "This is not an admin account. Use the User login.");
                return View(dto);
            }
            if (@as == "user" && roleName == "Admin")
            {
                ModelState.AddModelError("", "Admin accounts must use the Admin login.");
                return View(dto);
            }

            // No payment block — users can always log in.
            // Unverified users just see blurred rooms until they pay.

            await SignInUserAsync(user, dto.RememberMe);

            TempData["Success"] = $"Welcome back, {user.UserName}!";
            return RedirectToAction("Index", "Home");
        }

        // ── LOGOUT ──────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            Response.Cookies.Delete("UserMode");
            return RedirectToAction(nameof(RoleSelect));
        }

        [HttpGet]
        public IActionResult AccessDenied() => View();

        // ── REFRESH SESSION (called after admin verifies payment) ────────

        /// <summary>
        /// Re-issues the auth cookie with fresh claims (including updated IsPaymentVerified).
        /// Called automatically when the user visits /Payment/RegistrationFee after admin
        /// has already verified their payment and the old cookie still has PaymentVerified=False.
        /// </summary>
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> RefreshSession()
        {
            int userId = GetCurrentUserId();
            var user   = await _userService.GetUserByIdAsync(userId);
            if (user == null) return RedirectToAction(nameof(Login));

            await SignInUserAsync(user, rememberMe: false);
            TempData["Success"] = "Your account is now unlocked! All rooms are visible.";
            return RedirectToAction("Index", "Room");
        }

        // ── SET MODE ────────────────────────────────────────────

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public IActionResult SetMode(string mode, string? returnUrl = null)
        {
            if (mode != "owner" && mode != "seeker") mode = "owner";
            Response.Cookies.Append("UserMode", mode, new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddHours(8),
                HttpOnly = false,
                SameSite = SameSiteMode.Lax
            });
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction("Index", "Home");
        }

        // ── PROFILE ─────────────────────────────────────────────

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            int userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction(nameof(Login));

            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null) return NotFound();

            ViewBag.PostedRooms        = await _roomService.GetMyRoomsAsync(userId);
            ViewBag.Selections         = await _roomService.GetMySelectionsAsync(userId);
            ViewBag.IncomingSelections = await _roomService.GetIncomingSelectionsAsync(userId);

            return View(user);
        }

        // ── EDIT PROFILE ────────────────────────────────────────

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> EditProfile()
        {
            int userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction(nameof(Login));

            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null) return NotFound();

            ViewBag.Email              = user.Email;
            ViewBag.ProfilePicturePath = user.ProfilePicturePath;
            return View(new EditProfileDto
            {
                FullName    = user.FullName,
                UserName    = user.UserName,
                DateOfBirth = user.DateOfBirth,
                PhoneNumber = user.PhoneNumber,
                Address     = user.Address
            });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(EditProfileDto dto)
        {
            int userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction(nameof(Login));

            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null) return NotFound();

            ViewBag.Email              = user.Email;
            ViewBag.ProfilePicturePath = user.ProfilePicturePath;

            if (!ModelState.IsValid) return View(dto);

            user.FullName    = dto.FullName;
            user.UserName    = dto.UserName;
            user.DateOfBirth = dto.DateOfBirth;
            user.PhoneNumber = dto.PhoneNumber;
            user.Address     = dto.Address;

            string result = await _userService.UpdateUserAsync(user);
            if (result != "Success")
            {
                ModelState.AddModelError("", result);
                return View(dto);
            }

            TempData["Success"] = "Your profile has been updated.";
            return RedirectToAction(nameof(Profile));
        }

        // ── CHANGE PASSWORD ─────────────────────────────────────

        [HttpGet]
        [Authorize]
        public IActionResult ChangePassword() => View(new ChangePasswordDto());

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
        {
            if (!ModelState.IsValid) return View(dto);

            int userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction(nameof(Login));

            string result = await _authService.ChangePasswordAsync(userId, dto);
            if (result != "Success")
            {
                ModelState.AddModelError("", result);
                return View(dto);
            }

            TempData["Success"] = "Your password has been changed successfully.";
            return RedirectToAction(nameof(Profile));
        }

        // ── UPLOAD PROFILE PICTURE ──────────────────────────────

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(6 * 1024 * 1024)]
        public async Task<IActionResult> UploadProfilePicture(IFormFile? profilePicture)
        {
            int userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction(nameof(Login));

            if (profilePicture == null || profilePicture.Length == 0)
            {
                TempData["Error"] = "Please select an image to upload.";
                return RedirectToAction(nameof(Profile));
            }
            if (profilePicture.Length > MaxUploadBytes)
            {
                TempData["Error"] = "Image size cannot exceed 5 MB.";
                return RedirectToAction(nameof(Profile));
            }

            string ext = Path.GetExtension(profilePicture.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
            {
                TempData["Error"] = "Only JPG, PNG, GIF, and WEBP images are allowed.";
                return RedirectToAction(nameof(Profile));
            }
            if (!profilePicture.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "File must be an image.";
                return RedirectToAction(nameof(Profile));
            }

            string webRoot      = !string.IsNullOrEmpty(_env.WebRootPath) ? _env.WebRootPath : Path.Combine(_env.ContentRootPath, "wwwroot");
            string uploadFolder = Path.Combine(webRoot, "uploads", "profile-pics");
            Directory.CreateDirectory(uploadFolder);

            string fileName = $"user_{userId}_{DateTime.UtcNow.Ticks}{ext}";
            string savePath = Path.Combine(uploadFolder, fileName);
            using (var stream = new FileStream(savePath, FileMode.Create))
                await profilePicture.CopyToAsync(stream);

            var existing = await _userService.GetUserByIdAsync(userId);
            if (existing != null && !string.IsNullOrEmpty(existing.ProfilePicturePath))
            {
                string oldFull = Path.Combine(webRoot, existing.ProfilePicturePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(oldFull)) { try { System.IO.File.Delete(oldFull); } catch { } }
            }

            await _userService.UpdateProfilePictureAsync(userId, $"/uploads/profile-pics/{fileName}");
            TempData["Success"] = "Profile picture updated.";
            return RedirectToAction(nameof(Profile));
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveProfilePicture()
        {
            int userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction(nameof(Login));

            var user = await _userService.GetUserByIdAsync(userId);
            if (user != null && !string.IsNullOrEmpty(user.ProfilePicturePath))
            {
                string webRoot = !string.IsNullOrEmpty(_env.WebRootPath) ? _env.WebRootPath : Path.Combine(_env.ContentRootPath, "wwwroot");
                string oldFull = Path.Combine(webRoot, user.ProfilePicturePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(oldFull)) { try { System.IO.File.Delete(oldFull); } catch { } }
                await _userService.UpdateProfilePictureAsync(userId, null);
                TempData["Success"] = "Profile picture removed.";
            }
            return RedirectToAction(nameof(Profile));
        }

        // ── FORGOT PASSWORD ─────────────────────────────────────

        [HttpGet]
        public IActionResult ForgotPassword() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
        {
            if (!ModelState.IsValid) return View(dto);

            string result = await _authService.StartPasswordResetAsync(dto.Email!);
            if (result != "Success")
            {
                ModelState.AddModelError("", result);
                return View(dto);
            }

            TempData["Success"] = $"If an account exists for {dto.Email}, we've sent a reset code.";
            return RedirectToAction(nameof(ResetPassword), new { email = dto.Email });
        }

        [HttpGet]
        public IActionResult ResetPassword(string? email)
        {
            if (string.IsNullOrWhiteSpace(email)) return RedirectToAction(nameof(ForgotPassword));
            return View(new ResetPasswordDto { Email = email });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
        {
            if (!ModelState.IsValid) return View(dto);

            string result = await _authService.ResetPasswordAsync(dto);
            if (result != "Success")
            {
                ModelState.AddModelError("", result);
                ViewBag.AllowResend = result.Contains("expired", StringComparison.OrdinalIgnoreCase);
                return View(dto);
            }

            TempData["Success"] = "Password reset successful. Please log in.";
            return RedirectToAction(nameof(Login), new { @as = "user" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendResetOtp(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["Error"] = "Email is required.";
                return RedirectToAction(nameof(ForgotPassword));
            }

            string result = await _authService.ResendPasswordResetOtpAsync(email);
            if (result != "Success") TempData["Error"] = result;
            else TempData["Success"] = $"A new reset code has been sent to {email}.";

            return RedirectToAction(nameof(ResetPassword), new { email });
        }

        // ── HELPERS ─────────────────────────────────────────────

        /// <summary>
        /// Issues an auth cookie. Includes PaymentVerified claim so views can
        /// show/blur content without hitting the database on every request.
        /// </summary>
        private async Task SignInUserAsync(User user, bool rememberMe)
        {
            string roleName = user.Role?.RoleName ?? string.Empty;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name,           user.UserName ?? string.Empty),
                new Claim(ClaimTypes.Email,           user.Email   ?? string.Empty),
                new Claim(ClaimTypes.Role,            roleName),
                new Claim("PaymentVerified",          user.IsPaymentVerified.ToString())
            };

            var identity  = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            DateTimeOffset expiry = rememberMe
                ? DateTimeOffset.UtcNow.AddDays(30)
                : DateTimeOffset.UtcNow.AddHours(8);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties { IsPersistent = rememberMe, ExpiresUtc = expiry });

            if (roleName != "Admin")
            {
                Response.Cookies.Append("UserMode", "owner", new CookieOptions
                {
                    Expires  = expiry,
                    HttpOnly = false,
                    SameSite = SameSiteMode.Lax
                });
            }
        }

        private int GetCurrentUserId()
        {
            var val = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(val, out int id) ? id : 0;
        }
    }
}
