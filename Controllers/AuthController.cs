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

        public AuthController(
            IAuthService authService,
            IRoleService roleService,
            IUserService userService,
            IRoomService roomService)
        {
            _authService = authService;
            _roleService = roleService;
            _userService = userService;
            _roomService = roomService;
        }

        // GET: /Auth/RoleSelect — landing page when not logged in
        [HttpGet]
        public IActionResult RoleSelect()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // GET: /Auth/Register?as=admin|user — no role dropdown anymore
        [HttpGet]
        public IActionResult Register(string? @as = null)
        {
            if (string.IsNullOrEmpty(@as))
            {
                @as = "user";
            }
            ViewBag.RegisterType = @as;
            return View();
        }

        // POST: /Auth/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterDto dto, string? @as = null)
        {
            if (string.IsNullOrEmpty(@as))
            {
                @as = "user";
            }
            ViewBag.RegisterType = @as;

            // Auto-assign role based on registration type — user does NOT pick
            var roles = await _roleService.GetAllRolesAsync();
            string targetRoleName = @as == "admin" ? "Admin" : "RoomOwner";
            var targetRole = roles.FirstOrDefault(r => r.RoleName == targetRoleName);

            if (targetRole == null)
            {
                ModelState.AddModelError("", $"The {targetRoleName} role is missing from the database. Run the migration first.");
                return View(dto);
            }

            dto.RoleId = targetRole.Id;
            ModelState.Remove(nameof(dto.RoleId)); // we set it ourselves; skip its validation

            if (!ModelState.IsValid)
            {
                return View(dto);
            }

            string result = await _authService.RegisterAsync(dto);

            if (result != "Success")
            {
                ModelState.AddModelError("", result);
                return View(dto);
            }

            TempData["Success"] = "Registration successful. Please log in.";
            return RedirectToAction(nameof(Login), new { @as = @as });
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

            if (!ModelState.IsValid)
            {
                return View(dto);
            }

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

            // Default mode = owner for non-admin users
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

        // GET: /Auth/AccessDenied
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // POST: /Auth/SetMode — toggle RoomOwner / RoomSeeker mode for current session
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public IActionResult SetMode(string mode, string? returnUrl = null)
        {
            if (mode != "owner" && mode != "seeker")
            {
                mode = "owner";
            }

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

        // GET: /Auth/Profile — user's history page
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            int userId = GetCurrentUserId();
            if (userId == 0)
            {
                return RedirectToAction(nameof(Login));
            }

            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            ViewBag.PostedRooms = await _roomService.GetMyRoomsAsync(userId);
            ViewBag.Selections = await _roomService.GetMySelectionsAsync(userId);

            return View(user);
        }

        private int GetCurrentUserId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(idClaim, out int id) ? id : 0;
        }
    }
}
