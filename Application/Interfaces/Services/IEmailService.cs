using Bachelor_s_Point.Models;

namespace Bachelor_s_Point.Application.Interfaces.Services
{
    /// <summary>
    /// Domain-level email service. Builds email content for business events
    /// and delegates the actual sending to ISmtpEmailService.
    /// </summary>
    public interface IEmailService
    {
        Task SendRoomSelectedNotificationAsync(Room room, User owner, User seeker, string? customMessage);
    }
}
