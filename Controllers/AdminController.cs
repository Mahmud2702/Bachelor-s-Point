using Bachelor_s_Point.Application.Interfaces.UnitOfWork;
using Bachelor_s_Point.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Bachelor_s_Point.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IUnitOfWork _uow;

        public AdminController(IUnitOfWork uow)
        {
            _uow = uow;
        }

        // ── LIST ─────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var admins = await _uow.AdminRepo.GetAllAsync();
            return View(admins);
        }

        // ── ADD ──────────────────────────────────────────────────
        [HttpGet]
        public IActionResult Add() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(string name, string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                TempData["Error"] = "Email and password are required.";
                return View();
            }

            var existing = await _uow.AdminRepo.GetByEmailAsync(email);
            if (existing != null)
            {
                TempData["Error"] = "An admin with this email already exists.";
                return View();
            }

            var hasher = new PasswordHasher<Admin>();
            var admin  = new Admin { Name = string.IsNullOrWhiteSpace(name) ? "Admin" : name, Email = email };
            admin.PasswordHash = hasher.HashPassword(admin, password);

            await _uow.AdminRepo.AddAsync(admin);
            await _uow.SaveAsync();

            TempData["Success"] = $"Admin '{admin.Name}' added successfully.";
            return RedirectToAction(nameof(Index));
        }

        // ── DELETE ───────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var all = await _uow.AdminRepo.GetAllAsync();
            if (all.Count <= 1)
            {
                TempData["Error"] = "Cannot delete the last admin account.";
                return RedirectToAction(nameof(Index));
            }

            var admin = await _uow.AdminRepo.GetByIdAsync(id);
            if (admin != null)
            {
                _uow.AdminRepo.Delete(admin);
                await _uow.SaveAsync();
                TempData["Success"] = "Admin account removed.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
