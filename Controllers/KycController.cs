using Bachelor_s_Point.Application.DTOs;
using Bachelor_s_Point.Application.Interfaces.Services;
using Bachelor_s_Point.Application.Interfaces.UnitOfWork;
using Bachelor_s_Point.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Bachelor_s_Point.Controllers
{
    [Authorize]
    public class KycController : Controller
    {
        private readonly IKycService _kycService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _env;

        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
        private const long MaxImageBytes = 5 * 1024 * 1024; // 5 MB

        public KycController(IKycService kycService, IUnitOfWork unitOfWork, IWebHostEnvironment env)
        {
            _kycService = kycService;
            _unitOfWork = unitOfWork;
            _env = env;
        }

        private int GetCurrentUserId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(idClaim, out int id) ? id : 0;
        }

        // ============================================================
        // USER — submit & check status
        // ============================================================

        // GET: /Kyc/Status — user sees their own verification status
        [HttpGet]
        public async Task<IActionResult> Status()
        {
            int userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Auth");

            var kyc = await _kycService.GetByUserIdAsync(userId);
            return View(kyc);
        }

        // GET: /Kyc/Submit
        [HttpGet]
        public async Task<IActionResult> Submit()
        {
            int userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Auth");

            var existing = await _kycService.GetByUserIdAsync(userId);
            if (existing != null && existing.Status == "Verified")
            {
                TempData["Success"] = "Your identity is already verified.";
                return RedirectToAction(nameof(Status));
            }
            if (existing != null && existing.Status == "Pending")
            {
                TempData["Error"] = "Your verification is already under review.";
                return RedirectToAction(nameof(Status));
            }

            // If rejected, prefill the form so the user can fix and resubmit
            if (existing != null && existing.Status == "Rejected")
            {
                ViewBag.RejectionReason = existing.RejectionReason;
                return View(new SubmitKycDto
                {
                    FullNameOnNid = existing.FullNameOnNid,
                    NidNumber = existing.NidNumber
                });
            }

            return View(new SubmitKycDto());
        }

        // POST: /Kyc/Submit
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(20 * 1024 * 1024)]
        public async Task<IActionResult> Submit(SubmitKycDto dto, IFormFile? nidFront, IFormFile? nidBack, IFormFile? userPhoto)
        {
            int userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Auth");

            if (!ModelState.IsValid) return View(dto);

            if (nidFront == null || nidFront.Length == 0)
                ModelState.AddModelError("", "NID front image is required.");
            if (userPhoto == null || userPhoto.Length == 0)
                ModelState.AddModelError("", "Your photo is required.");

            if (!ModelState.IsValid) return View(dto);

            string? frontPath = await SaveImageAsync(nidFront!, userId, "nid-front");
            if (frontPath == null)
            {
                ModelState.AddModelError("", "NID front image is invalid. Use JPG, PNG or WEBP under 5 MB.");
                return View(dto);
            }

            string? photoPath = await SaveImageAsync(userPhoto!, userId, "photo");
            if (photoPath == null)
            {
                ModelState.AddModelError("", "Your photo is invalid. Use JPG, PNG or WEBP under 5 MB.");
                return View(dto);
            }

            string? backPath = null;
            if (nidBack != null && nidBack.Length > 0)
            {
                backPath = await SaveImageAsync(nidBack, userId, "nid-back");
                if (backPath == null)
                {
                    ModelState.AddModelError("", "NID back image is invalid. Use JPG, PNG or WEBP under 5 MB.");
                    return View(dto);
                }
            }

            var kyc = new KycVerification
            {
                UserId = userId,
                FullNameOnNid = dto.FullNameOnNid!.Trim(),
                NidNumber = dto.NidNumber!.Trim(),
                NidFrontImagePath = frontPath,
                NidBackImagePath = backPath,
                UserPhotoPath = photoPath
            };

            string result = await _kycService.SubmitAsync(kyc);
            if (result != "Success")
            {
                ModelState.AddModelError("", result);
                return View(dto);
            }

            TempData["Success"] = "Your identity verification has been submitted. An admin will review it shortly.";
            return RedirectToAction(nameof(Status));
        }

        // ============================================================
        // ADMIN — review verifications
        // ============================================================

        // GET: /Kyc/Pending — list of all submissions (admin)
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Pending()
        {
            var all = await _kycService.GetAllAsync();
            ViewBag.PendingCount = all.Count(k => k.Status == "Pending");
            ViewBag.VerifiedCount = all.Count(k => k.Status == "Verified");
            ViewBag.RejectedCount = all.Count(k => k.Status == "Rejected");
            return View(all);
        }

        // GET: /Kyc/Review/5 — review one submission (admin)
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Review(int id)
        {
            var kyc = await _kycService.GetByIdAsync(id);
            if (kyc == null) return NotFound();
            return View(kyc);
        }

        // POST: /Kyc/Approve
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            int adminId = GetCurrentUserId();
            string result = await _kycService.ApproveAsync(id, adminId);
            TempData[result == "Success" ? "Success" : "Error"] =
                result == "Success" ? "Verification approved." : result;
            return RedirectToAction(nameof(Pending));
        }

        // POST: /Kyc/Reject
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string rejectionReason)
        {
            int adminId = GetCurrentUserId();
            string result = await _kycService.RejectAsync(id, adminId, rejectionReason);
            TempData[result == "Success" ? "Success" : "Error"] =
                result == "Success" ? "Verification rejected." : result;
            return RedirectToAction(nameof(Pending));
        }

        // ============================================================
        // ADMIN — login activity
        // ============================================================

        // GET: /Kyc/LoginActivity
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> LoginActivity()
        {
            ViewBag.TotalLogins = await _unitOfWork.LoginHistoryRepo.GetTotalLoginCountAsync();
            ViewBag.DistinctUsers = await _unitOfWork.LoginHistoryRepo.GetDistinctUserCountAsync();
            ViewBag.TodayLogins = await _unitOfWork.LoginHistoryRepo.GetTodayLoginCountAsync();

            var recent = await _unitOfWork.LoginHistoryRepo.GetRecentAsync(200);
            return View(recent);
        }

        // ============================================================
        // helpers
        // ============================================================

        private async Task<string?> SaveImageAsync(IFormFile file, int userId, string label)
        {
            if (file.Length == 0 || file.Length > MaxImageBytes) return null;

            string ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext)) return null;
            if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)) return null;

            string webRoot = !string.IsNullOrEmpty(_env.WebRootPath)
                ? _env.WebRootPath
                : Path.Combine(_env.ContentRootPath, "wwwroot");

            string folder = Path.Combine(webRoot, "uploads", "kyc");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            string fileName = $"kyc_{userId}_{label}_{DateTime.UtcNow.Ticks}{ext}";
            string fullPath = Path.Combine(folder, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/uploads/kyc/{fileName}";
        }
    }
}
