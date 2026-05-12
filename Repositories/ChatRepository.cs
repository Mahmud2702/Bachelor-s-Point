using Bachelor_s_Point.Application.Interfaces.Repositories;
using Bachelor_s_Point.Data;
using Bachelor_s_Point.Models;
using Microsoft.EntityFrameworkCore;

namespace Bachelor_s_Point.Repositories
{
    public class ChatRepository : BaseRepository<ChatMessage>, IChatRepository
    {
        public ChatRepository(AppDbContext context) : base(context) { }

        public async Task<List<ChatMessage>> GetThreadAsync(int userIdA, int userIdB)
        {
            return await _context.ChatMessages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Where(m =>
                    (m.SenderId == userIdA && m.ReceiverId == userIdB) ||
                    (m.SenderId == userIdB && m.ReceiverId == userIdA))
                .OrderBy(m => m.SentAt)
                .ToListAsync();
        }

        public async Task<List<ChatMessage>> GetAllMessagesForUserAsync(int userId)
        {
            return await _context.ChatMessages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Where(m => m.SenderId == userId || m.ReceiverId == userId)
                .OrderByDescending(m => m.SentAt)
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            return await _context.ChatMessages
                .Where(m => m.ReceiverId == userId && !m.IsRead)
                .CountAsync();
        }

        public async Task MarkThreadAsReadAsync(int currentUserId, int otherUserId)
        {
            var unread = await _context.ChatMessages
                .Where(m => m.SenderId == otherUserId && m.ReceiverId == currentUserId && !m.IsRead)
                .ToListAsync();

            foreach (var msg in unread)
            {
                msg.IsRead = true;
            }
        }
    }
}
