using Bachelor_s_Point.Application.Interfaces.Services;
using Bachelor_s_Point.Models;

namespace Bachelor_s_Point.Services
{
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

        public async Task SendOtpEmailAsync(string toEmail, string? toName, string otp, int validityMinutes)
        {
            string greeting = string.IsNullOrWhiteSpace(toName)
                ? "Hello,"
                : $"Hello {System.Net.WebUtility.HtmlEncode(toName)},";

            string subject = "Your Bachelor's Point verification code";

            string body = $@"
<html>
<body style='font-family: Arial, sans-serif; color:#333; background:#f6f8fb; padding:24px;'>
  <div style='max-width:560px; margin:0 auto; background:#fff; border-radius:12px; padding:32px; box-shadow:0 4px 14px rgba(44,62,80,0.06);'>
    <h2 style='color:#2c3e50; margin-top:0;'>{greeting}</h2>
    <p>Thanks for signing up at <b>Bachelor's Point</b>. To finish creating your account, please use the verification code below.</p>

    <div style='text-align:center; margin:28px 0;'>
      <div style='display:inline-block; background:#eff6ff; color:#1d4ed8; font-size:32px; letter-spacing:8px; font-weight:700; padding:18px 30px; border-radius:10px; border:1px solid #c9d3e0;'>
        {System.Net.WebUtility.HtmlEncode(otp)}
      </div>
    </div>

    <p>This code is valid for <b>{validityMinutes} minutes</b>. Enter it on the verification page to complete your registration.</p>

    <p style='color:#6b7280; font-size:14px; margin-top:24px;'>
      If you didn't request this code, you can safely ignore this email — no account has been created yet.
    </p>

    <hr style='border:none; border-top:1px solid #e3e8ef; margin:24px 0;'/>
    <p style='font-size:12px; color:#9ca3af; margin:0;'>This is an automated message from Bachelor's Point. Please do not reply.</p>
  </div>
</body>
</html>";

            await _smtp.SendAsync(toEmail, subject, body, isHtml: true);
        }

        public async Task SendRegistrationConfirmationAsync(string toEmail, string toName)
        {
            string safeName = System.Net.WebUtility.HtmlEncode(toName ?? "there");
            string subject = "Welcome to Bachelor's Point!";

            string body = $@"
<html>
<body style='font-family: Arial, sans-serif; color:#333; background:#f6f8fb; padding:24px;'>
  <div style='max-width:560px; margin:0 auto; background:#fff; border-radius:12px; padding:32px; box-shadow:0 4px 14px rgba(44,62,80,0.06);'>
    <h2 style='color:#15803d; margin-top:0;'>Welcome, {safeName}!</h2>
    <p>Your registration on <b>Bachelor's Point</b> has been completed successfully.</p>

    <p>You can now log in and start browsing or posting rooms.</p>

    <div style='text-align:center; margin:28px 0;'>
      <a href='#' style='display:inline-block; background:#2563eb; color:#fff; text-decoration:none; padding:12px 28px; border-radius:8px; font-weight:600;'>Go to Login</a>
    </div>

    <p style='color:#6b7280; font-size:14px;'>
      If you have any questions, feel free to reach out to our support team.
    </p>

    <hr style='border:none; border-top:1px solid #e3e8ef; margin:24px 0;'/>
    <p style='font-size:12px; color:#9ca3af; margin:0;'>This is an automated message from Bachelor's Point.</p>
  </div>
</body>
</html>";

            await _smtp.SendAsync(toEmail, subject, body, isHtml: true);
        }
    }
}
