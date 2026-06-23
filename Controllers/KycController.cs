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

        // GET: /Kyc/DownloadKyc/{id}
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DownloadKyc(int id)
        {
            var kyc = await _kycService.GetByIdAsync(id);
            if (kyc == null) return NotFound();

            string userName    = kyc.User?.UserName ?? "-";
            string email       = kyc.User?.Email ?? "-";
            string phone       = kyc.User?.PhoneNumber ?? "-";
            string address     = kyc.User?.Address ?? "-";
            string nameOnNid   = kyc.FullNameOnNid ?? "-";
            string nidNumber   = kyc.NidNumber ?? "-";
            string status      = kyc.Status ?? "-";
            string submitted   = kyc.SubmittedAt.ToString("dd-MMM-yyyy hh:mm tt");
            string reviewed    = kyc.ReviewedAt.HasValue ? kyc.ReviewedAt.Value.ToString("dd-MMM-yyyy hh:mm tt") : "-";
            string rejectNote  = !string.IsNullOrEmpty(kyc.RejectionReason) ? kyc.RejectionReason : "-";
            string now         = DateTime.Now.ToString("dd-MMM-yyyy hh:mm tt");

            string baseUrl = $"{Request.Scheme}://{Request.Host}";

            string html = $@"<!DOCTYPE html>
<html lang=""en"">
<head>
<meta charset=""UTF-8"">
<meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
<title>KYC Report — {userName}</title>
<style>
  * {{ box-sizing:border-box; margin:0; padding:0; }}
  body {{ font-family:'Segoe UI',Arial,sans-serif; color:#111844; background:#fff; padding:32px; }}
  .header {{ border-bottom:3px solid #4B5694; padding-bottom:14px; margin-bottom:22px; display:flex; justify-content:space-between; align-items:flex-end; }}
  .header h1 {{ font-size:1.35rem; font-weight:800; color:#111844; }}
  .header small {{ font-size:.78rem; color:#7288AE; }}
  .badge {{ display:inline-block; padding:3px 12px; border-radius:999px; font-size:.78rem; font-weight:700; }}
  .badge-verified {{ background:#d1fae5; color:#065f46; }}
  .badge-pending  {{ background:#fef3c7; color:#92400e; }}
  .badge-rejected {{ background:#fee2e2; color:#991b1b; }}
  section {{ margin-bottom:22px; }}
  section h2 {{ font-size:.82rem; font-weight:800; text-transform:uppercase; letter-spacing:.6px; color:#7288AE; margin-bottom:10px; border-bottom:1px solid #E7E1D4; padding-bottom:6px; }}
  table {{ width:100%; border-collapse:collapse; font-size:.9rem; }}
  td {{ padding:7px 10px; border-bottom:1px solid #f0ece4; }}
  td:first-child {{ color:#7288AE; width:160px; font-weight:600; }}
  td:last-child {{ font-weight:700; }}
  .doc-grid {{ display:grid; grid-template-columns:repeat(3, 1fr); gap:12px; margin-top:8px; }}
  .doc-box {{ border:1px solid #E7E1D4; border-radius:8px; overflow:hidden; }}
  .doc-box img {{ width:100%; display:block; max-height:200px; object-fit:cover; }}
  .doc-label {{ font-size:.72rem; color:#7288AE; padding:5px 8px; background:#f8f5ee; text-align:center; font-weight:600; }}
  .footer {{ margin-top:28px; border-top:1px solid #E7E1D4; padding-top:12px; font-size:.75rem; color:#9AA3C6; display:flex; justify-content:space-between; }}
  @media print {{
    body {{ padding:18px; }}
    @page {{ margin:1.2cm; }}
  }}
</style>
</head>
<body>
  <div class=""header"">
    <div>
      <h1>🏠 Bachelor's Point — KYC Verification Record</h1>
      <small>Downloaded: {now}</small>
    </div>
    <span class=""badge {(status == "Verified" ? "badge-verified" : status == "Rejected" ? "badge-rejected" : "badge-pending")}"">
      {status}
    </span>
  </div>

  <section>
    <h2>User Information</h2>
    <table>
      <tr><td>Username</td><td>{userName}</td></tr>
      <tr><td>Email</td><td>{email}</td></tr>
      <tr><td>Phone</td><td>{phone}</td></tr>
      <tr><td>Address</td><td>{address}</td></tr>
    </table>
  </section>

  <section>
    <h2>NID Information</h2>
    <table>
      <tr><td>Full Name on NID</td><td>{nameOnNid}</td></tr>
      <tr><td>NID Number</td><td>{nidNumber}</td></tr>
    </table>
  </section>

  <section>
    <h2>Verification Status</h2>
    <table>
      <tr><td>Status</td><td>{status}</td></tr>
      <tr><td>Submitted</td><td>{submitted}</td></tr>
      <tr><td>Reviewed</td><td>{reviewed}</td></tr>
      {(status == "Rejected" ? $"<tr><td>Rejection Reason</td><td>{rejectNote}</td></tr>" : "")}
    </table>
  </section>

  <section>
    <h2>Identity Documents</h2>
    <div class=""doc-grid"">
      {(!string.IsNullOrEmpty(kyc.NidFrontImagePath) ? $@"<div class=""doc-box""><img src=""{baseUrl}{kyc.NidFrontImagePath}"" /><div class=""doc-label"">NID — Front</div></div>" : "")}
      {(!string.IsNullOrEmpty(kyc.NidBackImagePath) ? $@"<div class=""doc-box""><img src=""{baseUrl}{kyc.NidBackImagePath}"" /><div class=""doc-label"">NID — Back</div></div>" : "")}
      {(!string.IsNullOrEmpty(kyc.UserPhotoPath) ? $@"<div class=""doc-box""><img src=""{baseUrl}{kyc.UserPhotoPath}"" /><div class=""doc-label"">User Photo</div></div>" : "")}
    </div>
  </section>

  <div class=""footer"">
    <span>Bachelor's Point — Confidential KYC Record</span>
    <span>Record ID: {id}</span>
  </div>

  <script>window.onload = () => window.print();</script>
</body>
</html>";

            return Content(html, "text/html");
        }
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> LoginActivity(int page = 1, int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize < 5) pageSize = 10;

            ViewBag.TotalLogins    = await _unitOfWork.LoginHistoryRepo.GetTotalLoginCountAsync();
            ViewBag.DistinctUsers  = await _unitOfWork.LoginHistoryRepo.GetDistinctUserCountAsync();
            ViewBag.TodayLogins    = await _unitOfWork.LoginHistoryRepo.GetTodayLoginCountAsync();

            var all    = await _unitOfWork.LoginHistoryRepo.GetRecentAsync(2000);
            int total  = all.Count;
            int pages  = (int)Math.Ceiling(total / (double)pageSize);
            if (page > pages && pages > 0) page = pages;

            var paged  = all.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.CurrentPage  = page;
            ViewBag.TotalPages   = pages;
            ViewBag.PageSize     = pageSize;
            ViewBag.TotalRecords = total;

            return View(paged);
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
