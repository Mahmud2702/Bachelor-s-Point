using Bachelor_s_Point.Application.Interfaces.Repositories;
using Bachelor_s_Point.Data;
using Bachelor_s_Point.Models;
using Microsoft.EntityFrameworkCore;

namespace Bachelor_s_Point.Repositories
{
    public class RoomRepository : BaseRepository<Room>, IRoomRepository
    {
        public RoomRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<List<Room>> GetAllAvailableWithOwnerAsync()
        {
            return await _context.Rooms
                .Include(r => r.Owner)
                .Where(r => r.IsAvailable)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Room>> GetAllWithOwnerAsync()
        {
            return await _context.Rooms
                .Include(r => r.Owner)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<Room?> GetRoomWithOwnerByIdAsync(int id)
        {
            return await _context.Rooms
                .Include(r => r.Owner)
                    .ThenInclude(o => o!.Role)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<List<Room>> GetRoomsByOwnerIdAsync(int ownerId)
        {
            return await _context.Rooms
                .Where(r => r.UserId == ownerId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Room>> SearchAsync(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                return await GetAllAvailableWithOwnerAsync();
            }

            return await _context.Rooms
                .Include(r => r.Owner)
                .Where(r => r.IsAvailable && (
                    (r.Title != null && r.Title.Contains(searchText)) ||
                    (r.Description != null && r.Description.Contains(searchText)) ||
                    (r.Location != null && r.Location.Contains(searchText))))
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }
    }
}
