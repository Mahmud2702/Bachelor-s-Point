using Bachelor_s_Point.Models;

namespace Bachelor_s_Point.Application.Interfaces.Repositories
{
    public interface IRoomSelectionRepository : IBaseRepository<RoomSelection>
    {
        Task<List<RoomSelection>> GetBySeekerIdAsync(int seekerUserId);

        Task<int> CountBySeekerIdAsync(int seekerUserId);

        /// <summary>Selections on rooms posted by the given owner.</summary>
        Task<List<RoomSelection>> GetByOwnerIdAsync(int ownerUserId);

        /// <summary>All selections on a specific room (for the owner to view).</summary>
        Task<List<RoomSelection>> GetByRoomIdAsync(int roomId);
    }
}
