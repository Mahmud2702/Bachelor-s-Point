using Bachelor_s_Point.Application.Interfaces.Repositories;
using Bachelor_s_Point.Data;
using Bachelor_s_Point.Models;
using Microsoft.EntityFrameworkCore;

namespace Bachelor_s_Point.Repositories
{
    public class UserRepository : BaseRepository<User>, IUserRepository
    {
        public UserRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email != null && u.Email.ToLower() == email.ToLower());
        }

        public async Task<List<User>> GetAllUsersWithRoleAsync()
        {
            return await _context.Users
                .Include(u => u.Role)
                .ToListAsync();
        }

        public async Task<User?> GetUserWithRoleByIdAsync(int id)
        {
            return await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == id);
        }
    }
}
