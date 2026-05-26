using Bachelor_s_Point.Application.DTOs;
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

        public async Task<(List<Room> Items, int TotalCount)> GetFilteredPagedAsync(RoomFilterDto filter, int pageSize)
        {
            int page = filter.Page < 1 ? 1 : filter.Page;
            if (pageSize < 1) pageSize = 9;

            // Approved rooms only — that requirement never changes
            var query = _context.Rooms
                .Include(r => r.Owner)
                .Include(r => r.Images)
                .Where(r => r.IsApproved)
                .AsQueryable();

            // Available-only toggle
            if (filter.AvailableOnly)
                query = query.Where(r => r.IsAvailable);

            // Free-text search (title / description / location)
            if (!string.IsNullOrWhiteSpace(filter.SearchText))
            {
                string s = filter.SearchText.Trim();
                query = query.Where(r =>
                    (r.Title != null && r.Title.Contains(s)) ||
                    (r.Description != null && r.Description.Contains(s)) ||
                    (r.Location != null && r.Location.Contains(s)));
            }

            // Division / District (structured)
            if (!string.IsNullOrWhiteSpace(filter.Division))
                query = query.Where(r => r.Division == filter.Division);

            if (!string.IsNullOrWhiteSpace(filter.District))
                query = query.Where(r => r.District == filter.District);

            // Price range (Price is the base room cost)
            if (filter.MinPrice.HasValue)
                query = query.Where(r => r.Price >= filter.MinPrice.Value);

            if (filter.MaxPrice.HasValue)
                query = query.Where(r => r.Price <= filter.MaxPrice.Value);

            // Amenity toggles — "has X" means a positive cost was entered for it
            if (filter.HasWifi)
                query = query.Where(r => r.WifiCost != null && r.WifiCost > 0);

            if (filter.HasMeal)
                query = query.Where(r => r.MealCostPerMonth != null && r.MealCostPerMonth > 0);

            if (filter.HasMaid)
                query = query.Where(r => r.MaidCostPerMonth != null && r.MaidCostPerMonth > 0);

            // Sorting
            query = filter.SortBy switch
            {
                "price_asc" => query.OrderBy(r => r.Price),
                "price_desc" => query.OrderByDescending(r => r.Price),
                "oldest" => query.OrderBy(r => r.CreatedAt),
                _ => query.OrderByDescending(r => r.CreatedAt) // "newest"
            };

            int total = await query.CountAsync();
            var items = await query
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