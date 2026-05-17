using Bachelor_s_Point.Application.DTOs;
using Bachelor_s_Point.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Bachelor_s_Point.Controllers
{
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;
        private readonly IRoleService _roleService;
        private readonly IUserService _userService;
        private readonly IRoomService _roomService;
        private readonly IWebHostEnvironment _env;

        private static readonly string[] AllowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private const long MaxUploadBytes = 5 * 1024 * 1024;  // 5 MB

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
            _env = env;
        }

        // GET: /Auth/RoleSelect
        [HttpGet]
        public IActionResult RoleSelect()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // GET: /Auth/Register?as=admin|user
        [HttpGet]
        public IActionResult Register(string? @as = null)
        {
            if (string.IsNullOrEmpty(@as)) @as = "user";
            ViewBag.RegisterType = @as;
            return View();
        }

        // POST: /Auth/Register — Step 1 of OTP flow: store pending + send OTP
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterDto dto, string? @as = null)
        {
            if (string.IsNullOrEmpty(@as)) @as = "user";
            ViewBag.RegisterType = @as;

            // Resolve target role
            var roles = await _roleService.GetAllRolesAsync();
            string targetRoleName = @as == "admin" ? "Admin" : "User";
            var targetRole = roles.FirstOrDefault(r => r.RoleName == targetRoleName);

            if (targetRole == null)
            {
                ModelState.AddModelError("", $"The {targetRoleName} role is missing from the database. Run the migration first.");
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

            TempData["Success"] = $"We've sent a 6-digit verification code to {dto.Email}. Enter it below to complete your registration.";
            return RedirectToAction(nameof(VerifyOtp), new { email = dto.Email });
        }

        // GET: /Auth/VerifyOtp?email=...
        [HttpGet]
        public IActionResult VerifyOtp(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return RedirectToAction(nameof(Register));
            }

            var dto = new VerifyOtpDto { Email = email };
            return View(dto);
        }

        // POST: /Auth/VerifyOtp
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyOtp(VerifyOtpDto dto)
        {
            if (!ModelState.IsValid) return View(dto);

            string result = await _authService.VerifyOtpAndCreateUserAsync(dto.Email!, dto.Otp!);
            if (result != "Success")
            {
                ModelState.AddModelError("", result);
                ViewBag.AllowResend = result.Contains("expired", StringComparison.OrdinalIgnoreCase);
                return View(dto);
            }

            TempData["Success"] = "Registration completed! Please log in.";
            return RedirectToAction(nameof(Login), new { @as = "user" });
        }

        // POST: /Auth/ResendOtp
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
            if (result != "Success")
            {
                TempData["Error"] = result;
            }
            else
            {
                TempData["Success"] = $"A new verification code has been sent to {email}.";
            }

            return RedirectToAction(nameof(VerifyOtp), new { email });
        }

        // GET: /Auth/Login?as=admin|user
        [HttpGet]
        public IActionResult Login(string? @as = null)
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            ViewBag.LoginType = @as;
            return View();
        }

        // POST: /Auth/Login
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

            string? userRoleName = user.Role?.RoleName;

            if (@as == "admin" && userRoleName != "Admin")
            {
                ModelState.AddModelError("", "This is not an admin account. Please use the User login.");
                return View(dto);
            }

            if (@as == "user" && userRoleName == "Admin")
            {
                ModelState.AddModelError("", "Admin accounts must use the Admin login.");
                return View(dto);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Role, userRoleName ?? string.Empty)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                });

            if (userRoleName != "Admin")
            {
                Response.Cookies.Append("UserMode", "owner", new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddHours(8),
                    HttpOnly = false,
                    SameSite = SameSiteMode.Lax
                });
            }

            TempData["Success"] = $"Welcome back, {user.UserName}!";
            return RedirectToAction("Index", "Home");
        }

        // POST: /Auth/Logout
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

        // POST: /Auth/SetMode
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
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        // GET: /Auth/Profile
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            int userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction(nameof(Login));

            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null) return NotFound();

            ViewBag.PostedRooms = await _roomService.GetMyRoomsAsync(userId);
            ViewBag.Selections = await _roomService.GetMySelectionsAsync(userId);
            ViewBag.IncomingSelections = await _roomService.GetIncomingSelectionsAsync(userId);

            return View(user);
        }

        // POST: /Auth/UploadProfilePicture
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

            string extension = Path.GetExtension(profilePicture.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
            {
                TempData["Error"] = "Only JPG, PNG, GIF, and WEBP images are allowed.";
                return RedirectToAction(nameof(Profile));
            }

            if (!profilePicture.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "File must be an image.";
                return RedirectToAction(nameof(Profile));
            }

            string webRoot = !string.IsNullOrEmpty(_env.WebRootPath)
                ? _env.WebRootPath
                : Path.Combine(_env.ContentRootPath, "wwwroot");

            if (!Directory.Exists(webRoot)) Directory.CreateDirectory(webRoot);

            string uploadsFolder = Path.Combine(webRoot, "uploads", "profile-pics");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            string fileName = $"user_{userId}_{DateTime.UtcNow.Ticks}{extension}";
            string savePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(savePath, FileMode.Create))
            {
                await profilePicture.CopyToAsync(stream);
            }

            var existing = await _userService.GetUserByIdAsync(userId);
            if (existing != null && !string.IsNullOrEmpty(existing.ProfilePicturePath))
            {
                string relative = existing.ProfilePicturePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                string oldFullPath = Path.Combine(webRoot, relative);
                if (System.IO.File.Exists(oldFullPath))
                {
                    try { System.IO.File.Delete(oldFullPath); } catch { }
                }
            }

            string relativeUrl = $"/uploads/profile-pics/{fileName}";
            await _userService.UpdateProfilePictureAsync(userId, relativeUrl);

            TempData["Success"] = "Profile picture updated.";
            return RedirectToAction(nameof(Profile));
        }

        // POST: /Auth/RemoveProfilePicture
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
                string webRoot = !string.IsNullOrEmpty(_env.WebRootPath)
                    ? _env.WebRootPath
                    : Path.Combine(_env.ContentRootPath, "wwwroot");

                string relative = user.ProfilePicturePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                string oldFullPath = Path.Combine(webRoot, relative);
                if (System.IO.File.Exists(oldFullPath))
                {
                    try { System.IO.File.Delete(oldFullPath); } catch { }
                }

                await _userService.UpdateProfilePictureAsync(userId, null);
                TempData["Success"] = "Profile picture removed.";
            }

            return RedirectToAction(nameof(Profile));
        }


        // ============================================================
        // FORGOT PASSWORD FLOW
        // ============================================================

        // GET: /Auth/ForgotPassword
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: /Auth/ForgotPassword
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

            TempData["Success"] = $"If an account exists for {dto.Email}, we've sent a 6-digit reset code. Check your inbox.";
            return RedirectToAction(nameof(ResetPassword), new { email = dto.Email });
        }

        // GET: /Auth/ResetPassword?email=...
        [HttpGet]
        public IActionResult ResetPassword(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return RedirectToAction(nameof(ForgotPassword));

            var dto = new ResetPasswordDto { Email = email };
            return View(dto);
        }

        // POST: /Auth/ResetPassword
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

            TempData["Success"] = "Password reset successful. Please log in with your new password.";
            return RedirectToAction(nameof(Login), new { @as = "user" });
        }

        // POST: /Auth/ResendResetOtp
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
            if (result != "Success")
                TempData["Error"] = result;
            else
                TempData["Success"] = $"A new password reset code has been sent to {email}.";

            return RedirectToAction(nameof(ResetPassword), new { email });
        }

        private int GetCurrentUserId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(idClaim, out int id) ? id : 0;
        }
    }
}
