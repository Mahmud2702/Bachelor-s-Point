using Bachelor_s_Point.Models;

namespace Bachelor_s_Point.Application.Interfaces.Services
{
    public interface IPaymentService
    {
        Task<string> SubmitRegistrationPaymentAsync(int userId, string transactionId);
        Task<string> SubmitRoomPaymentAsync(int userId, int roomId, string transactionId, decimal amount);
        Task<string> VerifyPaymentAsync(int paymentId);
        Task<string> VerifyPaymentByTranIdAsync(string transactionId);
        Task<string> RejectPaymentAsync(int paymentId, string? note);
        Task<List<Payment>> GetAllPendingAsync();
        Task<Payment?> GetRegistrationPaymentAsync(int userId);
        Task<Payment?> GetRoomPaymentAsync(int roomId);
        Task<int> CountPendingPaymentsAsync();
    }
}
