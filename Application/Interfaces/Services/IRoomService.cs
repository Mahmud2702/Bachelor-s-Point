using Bachelor_s_Point.Application.DTOs;
using Bachelor_s_Point.Models;

namespace Bachelor_s_Point.Application.Interfaces.Services
{
    public interface IRoomService
    {
        Task<List<Room>> GetAllAvailableRoomsAsync();

        Task<List<Room>> GetAllRoomsAsync();

        Task<Room?> GetRoomByIdAsync(int id);

        Task<List<Room>> GetMyRoomsAsync(int ownerUserId);

        Task<List<Room>> SearchAsync(string searchText);

        Task<string> CreateRoomAsync(CreateRoomDto roomDto, int ownerUserId);

        Task<string> UpdateRoomAsync(Room room, int currentUserId, bool isAdmin);

        Task<string> DeleteRoomAsync(int roomId, int currentUserId, bool isAdmin);

        /// <summary>
        /// Triggered when a Room Seeker selects a room.
        /// Persists a RoomSelection record AND sends owner notification email.
        /// </summary>
        Task<string> SelectRoomAsync(SelectRoomDto selection);

        /// <summary>
        /// Get all rooms the user has selected (for booking history).
        /// </summary>
        Task<List<RoomSelection>> GetMySelectionsAsync(int seekerUserId);
    }
}
