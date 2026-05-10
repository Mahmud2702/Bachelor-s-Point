using Bachelor_s_Point.Application.DTOs;
using Bachelor_s_Point.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

namespace Bachelor_s_Point.Controllers
{
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;
        private readonly IRoleService _roleService;

        public AuthController(IAuthService authService, IRoleService roleService)
        {
            _authService = authService;
            _roleService = roleService;
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

        // GET: /Auth/Register?as=admin|user
        [HttpGet]
        public async Task<IActionResult> Register(string? @as = null)
        {
            // Default to "user" if not specified — never default to admin for safety
            if (string.IsNullOrEmpty(@as))
            {
                @as = "user";
            }

            ViewBag.RegisterType = @as;
            await LoadRolesDropdown(@as, null);
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

            if (!ModelState.IsValid)
            {
                await LoadRolesDropdown(@as, dto.RoleId);
                return View(dto);
            }

            // Validate the chosen role matches the registration type
            var role = await _roleService.GetRoleByIdAsync(dto.RoleId);
            if (role == null)
            {
                ModelState.AddModelError("", "Invalid role selected.");
                await LoadRolesDropdown(@as, dto.RoleId);
                return View(dto);
            }

            bool isAdminRole = role.RoleName == "Admin";

            if (@as == "admin" && !isAdminRole)
            {
                ModelState.AddModelError("", "Admin registration requires the Admin role.");
                await LoadRolesDropdown(@as, dto.RoleId);
                return View(dto);
            }

            if (@as == "user" && isAdminRole)
            {
                ModelState.AddModelError("", "Admin role is not allowed for user registration.");
                await LoadRolesDropdown(@as, dto.RoleId);
                return View(dto);
            }

            string result = await _authService.RegisterAsync(dto);

            if (result != "Success")
            {
                ModelState.AddModelError("", result);
                await LoadRolesDropdown(@as, dto.RoleId);
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

            TempData["Success"] = $"Welcome back, {user.UserName}!";
            return RedirectToAction("Index", "Home");
        }

        // POST: /Auth/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(RoleSelect));
        }

        // GET: /Auth/AccessDenied
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        /// <summary>
        /// Load the role dropdown filtered by registration type:
        ///   "admin" → only Admin role
        ///   "user"  → RoomOwner + RoomSeeker (no Admin)
        /// </summary>
        private async Task LoadRolesDropdown(string registerType, int? selectedRoleId)
        {
            var roles = await _roleService.GetAllRolesAsync();

            IEnumerable<Bachelor_s_Point.Models.Role> filtered;
            if (registerType == "admin")
            {
                filtered = roles.Where(r => r.RoleName == "Admin");
            }
            else
            {
                filtered = roles.Where(r => r.RoleName != "Admin");
            }

            ViewBag.RoleList = new SelectList(filtered, "Id", "RoleName", selectedRoleId);
        }
    }
}
