using Bachelor_s_Point.Models;

namespace Bachelor_s_Point.Application.DTOs
{
    public class ConversationSummary
    {
        public User OtherUser { get; set; } = null!;
        public string LastMessage { get; set; } = string.Empty;
        public DateTime LastSentAt { get; set; }
        public int UnreadCount { get; set; }
        public bool LastMessageFromMe { get; set; }
    }
}
