using Bachelor_s_Point.Models;

namespace Bachelor_s_Point.Application.Interfaces.Repositories
{
    public interface IAdminRepository : IBaseRepository<Admin>
    {
        Task<Admin?> GetByEmailAsync(string email);
    }
}
