using Bachelor_s_Point.Models;

namespace Bachelor_s_Point.Application.Interfaces.Repositories
{
    public interface IRoleRepository : IBaseRepository<Role>
    {
        Task<Role?> GetRoleByNameAsync(string roleName);

        Task<bool> HasUsersAsync(int roleId);
    }
}
