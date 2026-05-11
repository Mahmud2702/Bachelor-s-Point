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

        Task<string> CreateRoomAsync(CreateRoomDto roomDto, int ownerUserId, bool autoApprove);
        Task<string> UpdateRoomAsync(Room room, int currentUserId, bool isAdmin);
        Task<string> DeleteRoomAsync(int roomId, int currentUserId, bool isAdmin);

        Task<string> SelectRoomAsync(SelectRoomDto selection);
        Task<List<RoomSelection>> GetMySelectionsAsync(int seekerUserId);

        /// <summary>Bookings on the given owner's rooms (incoming bookings).</summary>
        Task<List<RoomSelection>> GetReceivedBookingsAsync(int ownerUserId);

        /// <summary>Bookings made against a specific room (shown on Details to owner/admin).</summary>
        Task<List<RoomSelection>> GetBookingsForRoomAsync(int roomId);

        Task<List<Room>> GetPendingApprovalAsync();
        Task<string> ApproveRoomAsync(int roomId);

        Task<PagedResult<Room>> GetApprovedPagedAsync(string? searchText, int page, int pageSize);
    }
}
