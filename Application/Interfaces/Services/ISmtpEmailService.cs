namespace Bachelor_s_Point.Application.Interfaces.Services
{
    /// <summary>
    /// Low-level SMTP transport. Anything that just needs to push bytes
    /// to a mail server uses this.
    /// </summary>
    public interface ISmtpEmailService
    {
        Task SendAsync(string toAddress, string subject, string body, bool isHtml = true);
    }
}
