using Bachelor_s_Point.Models;

namespace Bachelor_s_Point.Application.Interfaces.Services
{
    public interface IRoleService
    {
        Task<List<Role>> GetAllRolesAsync();

        Task<Role?> GetRoleByIdAsync(int id);

        Task<string> CreateRoleAsync(Role role);

        Task<string> UpdateRoleAsync(Role role);

        Task<string> DeleteRoleAsync(int id);
    }
}
