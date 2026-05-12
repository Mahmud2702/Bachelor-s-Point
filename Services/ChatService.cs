using Bachelor_s_Point.Application.DTOs;
using Bachelor_s_Point.Application.Interfaces.Services;
using Bachelor_s_Point.Application.Interfaces.UnitOfWork;
using Bachelor_s_Point.Models;

namespace Bachelor_s_Point.Services
{
    public class ChatService : IChatService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ChatService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<List<ConversationSummary>> GetConversationsAsync(int userId)
        {
            var allMessages = await _unitOfWork.ChatRepo.GetAllMessagesForUserAsync(userId);

            // Group by the other person in the conversation
            var conversations = allMessages
                .GroupBy(m => m.SenderId == userId ? m.ReceiverId : m.SenderId)
                .Select(g =>
                {
                    var latest = g.OrderByDescending(m => m.SentAt).First();
                    var otherUser = latest.SenderId == userId ? latest.Receiver : latest.Sender;
                    return new ConversationSummary
                    {
                        OtherUser = otherUser!,
                        LastMessage = latest.Content,
                        LastSentAt = latest.SentAt,
                        LastMessageFromMe = latest.SenderId == userId,
                        UnreadCount = g.Count(m => m.ReceiverId == userId && !m.IsRead)
                    };
                })
                .Where(c => c.OtherUser != null)
                .OrderByDescending(c => c.LastSentAt)
                .ToList();

            return conversations;
        }

        public async Task<(List<ChatMessage> Messages, User? OtherUser)> GetThreadAsync(int currentUserId, int otherUserId)
        {
            var otherUser = await _unitOfWork.UserRepo.GetByIdAsync(otherUserId);
            if (otherUser == null)
            {
                return (new List<ChatMessage>(), null);
            }

            var messages = await _unitOfWork.ChatRepo.GetThreadAsync(currentUserId, otherUserId);

            // Mark messages as read where current user is receiver
            await _unitOfWork.ChatRepo.MarkThreadAsReadAsync(currentUserId, otherUserId);
            await _unitOfWork.SaveAsync();

            return (messages, otherUser);
        }

        public async Task<string> SendMessageAsync(int senderId, int receiverId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return "Message cannot be empty";

            if (senderId == receiverId)
                return "You cannot message yourself";

            var sender = await _unitOfWork.UserRepo.GetByIdAsync(senderId);
            if (sender == null) return "Sender not found";

            var receiver = await _unitOfWork.UserRepo.GetByIdAsync(receiverId);
            if (receiver == null) return "Receiver not found";

            if (content.Length > 2000)
                content = content.Substring(0, 2000);

            var msg = new ChatMessage
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Content = content.Trim(),
                SentAt = DateTime.Now,
                IsRead = false
            };

            await _unitOfWork.ChatRepo.AddAsync(msg);
            await _unitOfWork.SaveAsync();
            return "Success";
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            return await _unitOfWork.ChatRepo.GetUnreadCountAsync(userId);
        }
    }
}
