using Bachelor_s_Point.Application.Interfaces.Services;
using Bachelor_s_Point.Models;

namespace Bachelor_s_Point.Services
{
    /// <summary>
    /// Domain-level email service. Builds subject + body for known business events
    /// and delegates the actual SMTP transport to ISmtpEmailService.
    /// </summary>
    public class EmailService : IEmailService
    {
        private readonly ISmtpEmailService _smtp;

        public EmailService(ISmtpEmailService smtp)
        {
            _smtp = smtp;
        }

        public async Task SendRoomSelectedNotificationAsync(
            Room room, User owner, User seeker, string? customMessage)
        {
            string subject = $"Your room \"{room.Title}\" has been selected";

            string safeMessage = string.IsNullOrWhiteSpace(customMessage)
                ? "(no message provided)"
                : System.Net.WebUtility.HtmlEncode(customMessage);

            string body = $@"
<html>
<body style='font-family: Arial, sans-serif; color: #333;'>
    <h2 style='color:#2c3e50;'>Hello {System.Net.WebUtility.HtmlEncode(owner.UserName ?? "there")},</h2>
    <p>Good news! A user is interested in your bachelor room listing on <b>Bachelor's Point</b>.</p>

    <h3>Room Details</h3>
    <ul>
        <li><b>Title:</b> {System.Net.WebUtility.HtmlEncode(room.Title ?? "")}</li>
        <li><b>Location:</b> {System.Net.WebUtility.HtmlEncode(room.Location ?? "")}</li>
        <li><b>Price:</b> {room.Price}</li>
    </ul>

    <h3>Interested User</h3>
    <ul>
        <li><b>Name:</b> {System.Net.WebUtility.HtmlEncode(seeker.UserName ?? "")}</li>
        <li><b>Email:</b> {System.Net.WebUtility.HtmlEncode(seeker.Email ?? "")}</li>
        <li><b>Address:</b> {System.Net.WebUtility.HtmlEncode(seeker.Address ?? "(not provided)")}</li>
    </ul>

    <h3>Message</h3>
    <p style='padding:10px; background:#f4f4f4; border-left:4px solid #3498db;'>{safeMessage}</p>

    <p>Please get in touch with the user using the email above.</p>

    <hr/>
    <p style='font-size:12px; color:#888;'>This is an automated notification from Bachelor's Point.</p>
</body>
</html>";

            await _smtp.SendAsync(owner.Email!, subject, body, isHtml: true);
        }
    }
}
