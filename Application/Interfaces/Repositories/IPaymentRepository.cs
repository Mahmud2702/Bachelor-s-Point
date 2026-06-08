using Bachelor_s_Point.Models;

namespace Bachelor_s_Point.Application.Interfaces.Repositories
{
    public interface IPaymentRepository : IBaseRepository<Payment>
    {
        Task<List<Payment>> GetAllPendingAsync();
        Task<Payment?> GetRegistrationPaymentByUserIdAsync(int userId);
        Task<Payment?> GetRoomPaymentByRoomIdAsync(int roomId);
        Task<List<Payment>> GetByUserIdAsync(int userId);
    }
}
