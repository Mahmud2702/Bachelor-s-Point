

using Bachelor_s_Point.Models;

namespace Bachelor_s_Point.Application.Interfaces.Repositories
{
    public interface IRoomSelectionRepository : IBaseRepository<RoomSelection>
    {
        Task<List<RoomSelection>> GetBySeekerIdAsync(int seekerUserId);

        Task<int> CountBySeekerIdAsync(int seekerUserId);

        /// <summary>Bookings against rooms owned by the given owner.</summary>
        Task<List<RoomSelection>> GetByOwnerIdAsync(int ownerUserId);

        /// <summary>Bookings for a single room (used in Room/Details for the owner).</summary>
        Task<List<RoomSelection>> GetByRoomIdAsync(int roomId);
    }
}
