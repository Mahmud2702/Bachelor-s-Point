using Bachelor_s_Point.Application.Interfaces.Repositories;
using Bachelor_s_Point.Data;
using Bachelor_s_Point.Models;
using Microsoft.EntityFrameworkCore;

namespace Bachelor_s_Point.Repositories
{
    public class KycRepository : BaseRepository<KycVerification>, IKycRepository
    {
        public KycRepository(AppDbContext context) : base(context) { }

        public async Task<KycVerification?> GetByUserIdAsync(int userId)
        {
            return await _context.KycVerifications
                .Include(k => k.User)
                .FirstOrDefaultAsync(k => k.UserId == userId);
        }

        public async Task<List<KycVerification>> GetAllWithUserAsync()
        {
            return await _context.KycVerifications
                .Include(k => k.User)
                .OrderByDescending(k => k.SubmittedAt)
                .ToListAsync();
        }

        public async Task<List<KycVerification>> GetByStatusAsync(string status)
        {
            return await _context.KycVerifications
                .Include(k => k.User)
                .Where(k => k.Status == status)
                .OrderByDescending(k => k.SubmittedAt)
                .ToListAsync();
        }

        public async Task<KycVerification?> GetByIdWithUserAsync(int id)
        {
            return await _context.KycVerifications
                .Include(k => k.User)
                .FirstOrDefaultAsync(k => k.Id == id);
        }

        public async Task<int> CountByStatusAsync(string status)
        {
            return await _context.KycVerifications.Where(k => k.Status == status).CountAsync();
        }
    }
}
