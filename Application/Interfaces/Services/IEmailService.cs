using Bachelor_s_Point.Models;

namespace Bachelor_s_Point.Application.Interfaces.Services
{
    /// Domain-level email service. Builds email content for business events
    /// and delegates the actual sending to ISmtpEmailService.
    public interface IEmailService
    {
        Task SendRoomSelectedNotificationAsync(Room room, User owner, User seeker, string? customMessage);

        /// Send a 6-digit OTP for email verification during registration.
        Task SendOtpEmailAsync(string toEmail, string? toName, string otp, int validityMinutes);

        /// Send a welcome / confirmation email once registration succeeds.
        Task SendRegistrationConfirmationAsync(string toEmail, string toName);
    }
}
