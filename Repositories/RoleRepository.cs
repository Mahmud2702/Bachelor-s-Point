using Bachelor_s_Point.Application.Interfaces.Repositories;
using Bachelor_s_Point.Data;
using Bachelor_s_Point.Models;
using Microsoft.EntityFrameworkCore;

namespace Bachelor_s_Point.Repositories
{
    public class RoleRepository : BaseRepository<Role>, IRoleRepository
    {
        public RoleRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<Role?> GetRoleByNameAsync(string roleName)
        {
            return await _context.Roles
                .FirstOrDefaultAsync(r => r.RoleName != null && r.RoleName.ToLower() == roleName.ToLower());
        }

        public async Task<bool> HasUsersAsync(int roleId)
        {
            return await _context.Users.AnyAsync(u => u.RoleId == roleId);
        }
    }
}
