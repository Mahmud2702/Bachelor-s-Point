using Bachelor_s_Point.Models;

namespace Bachelor_s_Point.Application.Interfaces.Services
{
    public interface IKycService
    {
        Task<KycVerification?> GetByUserIdAsync(int userId);

        /// <summary>True only if the user has a KYC record with Status == "Verified".</summary>
        Task<bool> IsUserVerifiedAsync(int userId);

        /// <summary>Insert a new KYC record, or overwrite a Rejected one (resubmission).</summary>
        Task<string> SubmitAsync(KycVerification kyc);

        Task<List<KycVerification>> GetAllAsync();
        Task<List<KycVerification>> GetByStatusAsync(string status);
        Task<KycVerification?> GetByIdAsync(int id);
        Task<int> CountPendingAsync();

        Task<string> ApproveAsync(int kycId, int adminId);
        Task<string> RejectAsync(int kycId, int adminId, string reason);
    }
}
