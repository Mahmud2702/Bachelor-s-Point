using Bachelor_s_Point.Application.Interfaces.Repositories;
using Bachelor_s_Point.Data;
using Bachelor_s_Point.Models;
using Microsoft.EntityFrameworkCore;

namespace Bachelor_s_Point.Repositories
{
    public class RoomSelectionRepository : BaseRepository<RoomSelection>, IRoomSelectionRepository
    {
        public RoomSelectionRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<List<RoomSelection>> GetBySeekerIdAsync(int seekerUserId)
        {
            return await _context.RoomSelections
                .Include(s => s.Room)
                    .ThenInclude(r => r!.Owner)
                .Where(s => s.SeekerUserId == seekerUserId)
                .OrderByDescending(s => s.SelectedAt)
                .ToListAsync();
        }

        public async Task<int> CountBySeekerIdAsync(int seekerUserId)
        {
            return await _context.RoomSelections
                .Where(s => s.SeekerUserId == seekerUserId)
                .CountAsync();
        }
    }
}
