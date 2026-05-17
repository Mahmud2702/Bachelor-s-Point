using Bachelor_s_Point.Models;

namespace Bachelor_s_Point.Application.Interfaces.Services
{
    public interface IEmailService
    {
        Task SendRoomSelectedNotificationAsync(Room room, User owner, User seeker, string? customMessage);
        Task SendOtpEmailAsync(string toEmail, string? toName, string otp, int validityMinutes);
        Task SendRegistrationConfirmationAsync(string toEmail, string toName);

        /// <summary>OTP email for forgot-password flow.</summary>
        Task SendPasswordResetOtpAsync(string toEmail, string? toName, string otp, int validityMinutes);

        /// <summary>Confirmation that password has been changed.</summary>
        Task SendPasswordChangedConfirmationAsync(string toEmail, string? toName);
    }
}
