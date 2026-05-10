using Bachelor_s_Point.Application.Interfaces.Services;
using Bachelor_s_Point.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Bachelor_s_Point.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        private readonly IUserService _userService;
        private readonly IRoleService _roleService;

        public UserController(IUserService userService, IRoleService roleService)
        {
            _userService = userService;
            _roleService = roleService;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _userService.GetAllUsersAsync();
            return View(users);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var user = await _userService.GetUserByIdAsync(id.Value);

            if (user == null) return NotFound();

            return View(user);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var user = await _userService.GetUserByIdAsync(id.Value);

            if (user == null) return NotFound();

            user.PasswordHash = "";

            await LoadRolesDropdown(user.RoleId);
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, User user)
        {
            if (id != user.Id) return NotFound();

            ModelState.Remove("Role");
            ModelState.Remove("Rooms");
            ModelState.Remove("PasswordHash");

            if (ModelState.IsValid)
            {
                string result = await _userService.UpdateUserAsync(user);

                if (result == "Success")
                {
                    TempData["Success"] = "User updated";
                    return RedirectToAction(nameof(Index));
                }

                ModelState.AddModelError("", result);
            }

            await LoadRolesDropdown(user.RoleId);
            return View(user);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var user = await _userService.GetUserByIdAsync(id.Value);

            if (user == null) return NotFound();

            return View(user);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _userService.DeleteUserAsync(id);
            TempData["Success"] = "User deleted";
            return RedirectToAction(nameof(Index));
        }

        private async Task LoadRolesDropdown(int? selectedRoleId = null)
        {
            var roles = await _roleService.GetAllRolesAsync();
            ViewBag.RoleList = new SelectList(roles, "Id", "RoleName", selectedRoleId);
        }
    }
}
