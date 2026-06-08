using Bachelor_s_Point.Application.Interfaces.Repositories;
using Bachelor_s_Point.Data;
using Bachelor_s_Point.Models;
using Microsoft.EntityFrameworkCore;

namespace Bachelor_s_Point.Repositories
{
    public class PaymentRepository : BaseRepository<Payment>, IPaymentRepository
    {
        public PaymentRepository(AppDbContext context) : base(context) { }

        public async Task<List<Payment>> GetAllPendingAsync()
        {
            return await _context.Payments
                .Include(p => p.User)
                .Include(p => p.Room)
                .Where(p => p.Status == PaymentStatus.Pending)
                .OrderBy(p => p.SubmittedAt)
                .ToListAsync();
        }

        public async Task<Payment?> GetRegistrationPaymentByUserIdAsync(int userId)
        {
            return await _context.Payments
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.UserId == userId && p.Type == PaymentType.Registration);
        }

        public async Task<Payment?> GetRoomPaymentByRoomIdAsync(int roomId)
        {
            return await _context.Payments
                .Include(p => p.User)
                .Include(p => p.Room)
                .FirstOrDefaultAsync(p => p.RoomId == roomId && p.Type == PaymentType.RoomPosting);
        }

        public async Task<List<Payment>> GetByUserIdAsync(int userId)
        {
            return await _context.Payments
                .Include(p => p.Room)
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.SubmittedAt)
                .ToListAsync();
        }
    }
}
