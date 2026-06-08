using Bachelor_s_Point.Application.Interfaces.Services;
using Bachelor_s_Point.Application.Interfaces.UnitOfWork;
using Bachelor_s_Point.Models;

namespace Bachelor_s_Point.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(IUnitOfWork unitOfWork, ILogger<PaymentService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<string> SubmitRegistrationPaymentAsync(int userId, string transactionId)
        {
            if (string.IsNullOrWhiteSpace(transactionId))
                return "Transaction ID is required";

            var user = await _unitOfWork.UserRepo.GetByIdAsync(userId);
            if (user == null) return "User not found";

            var existing = await _unitOfWork.PaymentRepo.GetRegistrationPaymentByUserIdAsync(userId);
            if (existing != null)
            {
                if (existing.Status == PaymentStatus.Verified)
                    return "Your registration payment is already verified.";

                // Pending or Rejected — allow update/resubmit
                existing.TransactionId = transactionId.Trim();
                existing.Status        = PaymentStatus.Pending;
                existing.SubmittedAt   = DateTime.Now;
                existing.AdminNote     = null;
                _unitOfWork.PaymentRepo.Update(existing);
                await _unitOfWork.SaveAsync();
                return "Success";
            }

            var payment = new Payment
            {
                UserId        = userId,
                Type          = PaymentType.Registration,
                Amount        = 20,
                TransactionId = transactionId.Trim(),
                Status        = PaymentStatus.Pending,
                SubmittedAt   = DateTime.Now
            };
            await _unitOfWork.PaymentRepo.AddAsync(payment);
            await _unitOfWork.SaveAsync();
            return "Success";
        }

        public async Task<string> SubmitRoomPaymentAsync(int userId, int roomId, string transactionId, decimal amount)
        {
            if (string.IsNullOrWhiteSpace(transactionId))
                return "Transaction ID is required";

            var room = await _unitOfWork.RoomRepo.GetByIdAsync(roomId);
            if (room == null) return "Room not found";
            if (room.UserId != userId) return "You are not the owner of this room";

            var existing = await _unitOfWork.PaymentRepo.GetRoomPaymentByRoomIdAsync(roomId);
            if (existing != null)
            {
                if (existing.Status == PaymentStatus.Verified)
                    return "Payment for this room is already verified.";

                existing.TransactionId = transactionId.Trim();
                existing.Amount        = amount;
                existing.Status        = PaymentStatus.Pending;
                existing.SubmittedAt   = DateTime.Now;
                existing.AdminNote     = null;
                _unitOfWork.PaymentRepo.Update(existing);
                await _unitOfWork.SaveAsync();
                return "Success";
            }

            var payment = new Payment
            {
                UserId        = userId,
                Type          = PaymentType.RoomPosting,
                Amount        = amount,
                TransactionId = transactionId.Trim(),
                RoomId        = roomId,
                Status        = PaymentStatus.Pending,
                SubmittedAt   = DateTime.Now
            };
            await _unitOfWork.PaymentRepo.AddAsync(payment);
            await _unitOfWork.SaveAsync();
            return "Success";
        }

        public async Task<string> VerifyPaymentAsync(int paymentId)
        {
            var payment = await _unitOfWork.PaymentRepo.GetByIdAsync(paymentId);
            if (payment == null) return "Payment not found";
            if (payment.Status == PaymentStatus.Verified) return "Already verified";

            payment.Status     = PaymentStatus.Verified;
            payment.VerifiedAt = DateTime.Now;
            _unitOfWork.PaymentRepo.Update(payment);

            // Side-effects
            if (payment.Type == PaymentType.Registration)
            {
                var user = await _unitOfWork.UserRepo.GetByIdAsync(payment.UserId);
                if (user != null)
                {
                    user.IsPaymentVerified = true;
                    _unitOfWork.UserRepo.Update(user);
                }
            }
            else if (payment.Type == PaymentType.RoomPosting && payment.RoomId.HasValue)
            {
                var room = await _unitOfWork.RoomRepo.GetByIdAsync(payment.RoomId.Value);
                if (room != null && !room.IsApproved)
                {
                    room.IsApproved  = true;
                    room.ApprovedAt  = DateTime.Now;
                    _unitOfWork.RoomRepo.Update(room);
                }
            }

            await _unitOfWork.SaveAsync();
            return "Success";
        }

        public async Task<string> RejectPaymentAsync(int paymentId, string? note)
        {
            var payment = await _unitOfWork.PaymentRepo.GetByIdAsync(paymentId);
            if (payment == null) return "Payment not found";

            payment.Status    = PaymentStatus.Rejected;
            payment.AdminNote = note?.Trim();
            _unitOfWork.PaymentRepo.Update(payment);
            await _unitOfWork.SaveAsync();
            return "Success";
        }

        public async Task<List<Payment>> GetAllPendingAsync()
            => await _unitOfWork.PaymentRepo.GetAllPendingAsync();

        public async Task<Payment?> GetRegistrationPaymentAsync(int userId)
            => await _unitOfWork.PaymentRepo.GetRegistrationPaymentByUserIdAsync(userId);

        public async Task<Payment?> GetRoomPaymentAsync(int roomId)
            => await _unitOfWork.PaymentRepo.GetRoomPaymentByRoomIdAsync(roomId);

        public async Task<int> CountPendingPaymentsAsync()
        {
            var list = await _unitOfWork.PaymentRepo.GetAllPendingAsync();
            return list.Count;
        }
    }
}
