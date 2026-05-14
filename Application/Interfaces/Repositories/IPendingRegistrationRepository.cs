using Bachelor_s_Point.Models;

namespace Bachelor_s_Point.Application.Interfaces.Repositories
{
    public interface IPendingRegistrationRepository : IBaseRepository<PendingRegistration>
    {
        Task<PendingRegistration?> GetByEmailAsync(string email);
    }
}
