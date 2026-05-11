using Bachelor_s_Point.Application.Interfaces.Repositories;
using Bachelor_s_Point.Data;
using Bachelor_s_Point.Models;
using Microsoft.EntityFrameworkCore;

namespace Bachelor_s_Point.Repositories
{
    public class RoomRepository : BaseRepository<Room>, IRoomRepository
    {
        public RoomRepository(AppDbContext context) : base(context) { }

        public async Task<List<Room>> GetAllAvailableWithOwnerAsync()
        {
            return await _context.Rooms
                .Include(r => r.Owner)
                .Include(r => r.Images)
                .Where(r => r.IsAvailable && r.IsApproved)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Room>> GetAllWithOwnerAsync()
        {
            return await _context.Rooms
                .Include(r => r.Owner)
                .Include(r => r.Images)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<Room?> GetRoomWithOwnerByIdAsync(int id)
        {
            return await _context.Rooms
                .Include(r => r.Owner)
                    .ThenInclude(o => o!.Role)
                .Include(r => r.Images)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<List<Room>> GetRoomsByOwnerIdAsync(int ownerId)
        {
            return await _context.Rooms
                .Include(r => r.Images)
                .Where(r => r.UserId == ownerId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Room>> SearchAsync(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return await GetAllAvailableWithOwnerAsync();

            return await _context.Rooms
                .Include(r => r.Owner)
                .Include(r => r.Images)
                .Where(r => r.IsAvailable && r.IsApproved && (
                    (r.Title != null && r.Title.Contains(searchText)) ||
                    (r.Description != null && r.Description.Contains(searchText)) ||
                    (r.Location != null && r.Location.Contains(searchText))))
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<(List<Room> Items, int TotalCount)> GetApprovedAvailablePagedAsync(string? searchText, int page, int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            var query = _context.Rooms
                .Include(r => r.Owner)
                .Include(r => r.Images)
                .Where(r => r.IsAvailable && r.IsApproved);

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                query = query.Where(r =>
                    (r.Title != null && r.Title.Contains(searchText)) ||
                    (r.Description != null && r.Description.Contains(searchText)) ||
                    (r.Location != null && r.Location.Contains(searchText)));
            }

            int total = await query.CountAsync();
            var items = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, total);
        }

        public async Task<List<Room>> GetPendingApprovalAsync()
        {
            return await _context.Rooms
                .Include(r => r.Owner)
                .Include(r => r.Images)
                .Where(r => !r.IsApproved)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }
    }
}
