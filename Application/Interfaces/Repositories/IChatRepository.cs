using Bachelor_s_Point.Models;

namespace Bachelor_s_Point.Application.Interfaces.Repositories
{
    public interface IChatRepository : IBaseRepository<ChatMessage>
    {
        Task<List<ChatMessage>> GetThreadAsync(int userIdA, int userIdB);

        Task<List<ChatMessage>> GetAllMessagesForUserAsync(int userId);

        Task<int> GetUnreadCountAsync(int userId);

        Task MarkThreadAsReadAsync(int currentUserId, int otherUserId);
    }
}
