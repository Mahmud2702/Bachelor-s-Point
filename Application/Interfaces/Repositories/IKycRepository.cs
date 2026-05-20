using Bachelor_s_Point.Models;

namespace Bachelor_s_Point.Application.Interfaces.Repositories
{
    public interface IKycRepository : IBaseRepository<KycVerification>
    {
        Task<KycVerification?> GetByUserIdAsync(int userId);
        Task<List<KycVerification>> GetAllWithUserAsync();
        Task<List<KycVerification>> GetByStatusAsync(string status);
        Task<KycVerification?> GetByIdWithUserAsync(int id);
        Task<int> CountByStatusAsync(string status);
    }
}
