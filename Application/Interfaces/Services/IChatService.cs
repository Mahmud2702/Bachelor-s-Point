using Bachelor_s_Point.Application.DTOs;
using Bachelor_s_Point.Models;

namespace Bachelor_s_Point.Application.Interfaces.Services
{
    public interface IChatService
    {
        Task<List<ConversationSummary>> GetConversationsAsync(int userId);

        Task<(List<ChatMessage> Messages, User? OtherUser)> GetThreadAsync(int currentUserId, int otherUserId);

        Task<string> SendMessageAsync(int senderId, int receiverId, string content);

        Task<int> GetUnreadCountAsync(int userId);
    }
}
