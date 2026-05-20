using Bachelor_s_Point.Application.Interfaces.Repositories;
using Bachelor_s_Point.Data;
using Bachelor_s_Point.Models;
using Microsoft.EntityFrameworkCore;

namespace Bachelor_s_Point.Repositories
{
    public class LoginHistoryRepository : BaseRepository<LoginHistory>, ILoginHistoryRepository
    {
        public LoginHistoryRepository(AppDbContext context) : base(context) { }

        public async Task<List<LoginHistory>> GetRecentAsync(int take = 200)
        {
            return await _context.LoginHistories
                .Include(l => l.User)
                .OrderByDescending(l => l.LoginAt)
                .Take(take)
                .ToListAsync();
        }

        public async Task<int> GetTotalLoginCountAsync()
        {
            return await _context.LoginHistories.CountAsync();
        }

        public async Task<int> GetDistinctUserCountAsync()
        {
            return await _context.LoginHistories
                .Select(l => l.UserId)
                .Distinct()
                .CountAsync();
        }

        public async Task<int> GetTodayLoginCountAsync()
        {
            DateTime todayStart = DateTime.Now.Date;
            DateTime todayEnd = todayStart.AddDays(1);
            return await _context.LoginHistories
                .Where(l => l.LoginAt >= todayStart && l.LoginAt < todayEnd)
                .CountAsync();
        }
    }
}
