using Bachelor_s_Point.Application.Interfaces.Repositories;
using Bachelor_s_Point.Data;
using Bachelor_s_Point.Models;
using Microsoft.EntityFrameworkCore;

namespace Bachelor_s_Point.Repositories
{
    public class PasswordResetRepository : BaseRepository<PasswordResetToken>, IPasswordResetRepository
    {
        public PasswordResetRepository(AppDbContext context) : base(context) { }

        public async Task<PasswordResetToken?> GetByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return null;
            return await _context.PasswordResetTokens
                .FirstOrDefaultAsync(p => p.Email.ToLower() == email.ToLower());
        }
    }
}
