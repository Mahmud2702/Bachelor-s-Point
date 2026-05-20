using Bachelor_s_Point.Application.Interfaces.Services;
using Bachelor_s_Point.Application.Interfaces.UnitOfWork;
using Bachelor_s_Point.Models;

namespace Bachelor_s_Point.Services
{
    public class KycService : IKycService
    {
        private readonly IUnitOfWork _unitOfWork;

        public KycService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<KycVerification?> GetByUserIdAsync(int userId)
            => await _unitOfWork.KycRepo.GetByUserIdAsync(userId);

        public async Task<bool> IsUserVerifiedAsync(int userId)
        {
            var kyc = await _unitOfWork.KycRepo.GetByUserIdAsync(userId);
            return kyc != null && kyc.Status == "Verified";
        }

        public async Task<string> SubmitAsync(KycVerification kyc)
        {
            if (kyc == null) return "Invalid submission";

            var existing = await _unitOfWork.KycRepo.GetByUserIdAsync(kyc.UserId);
            if (existing != null)
            {
                // Already verified — block re-submission
                if (existing.Status == "Verified")
                    return "Your identity is already verified.";

                // Pending — wait for review
                if (existing.Status == "Pending")
                    return "Your verification is already under review.";

                // Rejected — allow resubmission by overwriting the old record
                existing.FullNameOnNid = kyc.FullNameOnNid;
                existing.NidNumber = kyc.NidNumber;
                existing.NidFrontImagePath = kyc.NidFrontImagePath;
                existing.NidBackImagePath = kyc.NidBackImagePath;
                existing.UserPhotoPath = kyc.UserPhotoPath;
                existing.Status = "Pending";
                existing.SubmittedAt = DateTime.Now;
                existing.ReviewedAt = null;
                existing.ReviewedByAdminId = null;
                existing.RejectionReason = null;
                _unitOfWork.KycRepo.Update(existing);
            }
            else
            {
                kyc.Status = "Pending";
                kyc.SubmittedAt = DateTime.Now;
                await _unitOfWork.KycRepo.AddAsync(kyc);
            }

            await _unitOfWork.SaveAsync();
            return "Success";
        }

        public async Task<List<KycVerification>> GetAllAsync()
            => await _unitOfWork.KycRepo.GetAllWithUserAsync();

        public async Task<List<KycVerification>> GetByStatusAsync(string status)
            => await _unitOfWork.KycRepo.GetByStatusAsync(status);

        public async Task<KycVerification?> GetByIdAsync(int id)
            => await _unitOfWork.KycRepo.GetByIdWithUserAsync(id);

        public async Task<int> CountPendingAsync()
            => await _unitOfWork.KycRepo.CountByStatusAsync("Pending");

        public async Task<string> ApproveAsync(int kycId, int adminId)
        {
            var kyc = await _unitOfWork.KycRepo.GetByIdAsync(kycId);
            if (kyc == null) return "Verification record not found";

            kyc.Status = "Verified";
            kyc.ReviewedAt = DateTime.Now;
            kyc.ReviewedByAdminId = adminId;
            kyc.RejectionReason = null;
            _unitOfWork.KycRepo.Update(kyc);
            await _unitOfWork.SaveAsync();
            return "Success";
        }

        public async Task<string> RejectAsync(int kycId, int adminId, string reason)
        {
            var kyc = await _unitOfWork.KycRepo.GetByIdAsync(kycId);
            if (kyc == null) return "Verification record not found";

            kyc.Status = "Rejected";
            kyc.ReviewedAt = DateTime.Now;
            kyc.ReviewedByAdminId = adminId;
            kyc.RejectionReason = string.IsNullOrWhiteSpace(reason) ? "Not specified" : reason.Trim();
            _unitOfWork.KycRepo.Update(kyc);
            await _unitOfWork.SaveAsync();
            return "Success";
        }
    }
}
