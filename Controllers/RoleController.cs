using Bachelor_s_Point.Application.Interfaces.Services;
using Bachelor_s_Point.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bachelor_s_Point.Controllers
{
    [Authorize(Roles = "Admin")]
    public class RoleController : Controller
    {
        private readonly IRoleService _roleService;

        public RoleController(IRoleService roleService)
        {
            _roleService = roleService;
        }

        public async Task<IActionResult> Index()
        {
            var roles = await _roleService.GetAllRolesAsync();
            return View(roles);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var role = await _roleService.GetRoleByIdAsync(id.Value);

            if (role == null) return NotFound();

            return View(role);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Role role)
        {
            ModelState.Remove("Users");

            if (ModelState.IsValid)
            {
                string result = await _roleService.CreateRoleAsync(role);

                if (result == "Success")
                {
                    TempData["Success"] = "Role created";
                    return RedirectToAction(nameof(Index));
                }

                ModelState.AddModelError("", result);
            }

            return View(role);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var role = await _roleService.GetRoleByIdAsync(id.Value);

            if (role == null) return NotFound();

            return View(role);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Role role)
        {
            if (id != role.Id) return NotFound();

            ModelState.Remove("Users");

            if (ModelState.IsValid)
            {
                string result = await _roleService.UpdateRoleAsync(role);

                if (result == "Success")
                {
                    TempData["Success"] = "Role updated";
                    return RedirectToAction(nameof(Index));
                }

                ModelState.AddModelError("", result);
            }

            return View(role);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var role = await _roleService.GetRoleByIdAsync(id.Value);

            if (role == null) return NotFound();

            return View(role);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            string result = await _roleService.DeleteRoleAsync(id);

            if (result != "Success")
            {
                TempData["Error"] = result;
            }
            else
            {
                TempData["Success"] = "Role deleted";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
