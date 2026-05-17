using Bachelor_s_Point.Models;

namespace Bachelor_s_Point.Application.Interfaces.Repositories
{
    public interface IPasswordResetRepository : IBaseRepository<PasswordResetToken>
    {
        Task<PasswordResetToken?> GetByEmailAsync(string email);
    }
}
