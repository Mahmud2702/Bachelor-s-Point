using Bachelor_s_Point.Models;

namespace Bachelor_s_Point.Application.Interfaces.Repositories
{
    public interface IRoomRepository : IBaseRepository<Room>
    {
        Task<List<Room>> GetAllAvailableWithOwnerAsync();

        Task<List<Room>> GetAllWithOwnerAsync();

        Task<Room?> GetRoomWithOwnerByIdAsync(int id);

        Task<List<Room>> GetRoomsByOwnerIdAsync(int ownerId);

        Task<List<Room>> SearchAsync(string searchText);

        /// <summary>Get approved+available rooms paged for Browse Rooms page.</summary>
        Task<(List<Room> Items, int TotalCount)> GetApprovedAvailablePagedAsync(string? searchText, int page, int pageSize);

        /// <summary>Get rooms waiting for admin approval.</summary>
        Task<List<Room>> GetPendingApprovalAsync();
    }
}
