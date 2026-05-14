using Bachelor_s_Point.Application.Interfaces.Repositories;
using Bachelor_s_Point.Data;
using Bachelor_s_Point.Models;
using Microsoft.EntityFrameworkCore;

namespace Bachelor_s_Point.Repositories
{
    public class PendingRegistrationRepository : BaseRepository<PendingRegistration>, IPendingRegistrationRepository
    {
        public PendingRegistrationRepository(AppDbContext context) : base(context) { }

        public async Task<PendingRegistration?> GetByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return null;
            return await _context.PendingRegistrations
                .FirstOrDefaultAsync(p => p.Email.ToLower() == email.ToLower());
        }
    }
}
