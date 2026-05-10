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
    }
}
