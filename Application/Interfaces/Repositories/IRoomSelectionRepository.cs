using Bachelor_s_Point.Models;

namespace Bachelor_s_Point.Application.Interfaces.Repositories
{
    public interface IRoomSelectionRepository : IBaseRepository<RoomSelection>
    {
        Task<List<RoomSelection>> GetBySeekerIdAsync(int seekerUserId);

        Task<int> CountBySeekerIdAsync(int seekerUserId);
    }
}
