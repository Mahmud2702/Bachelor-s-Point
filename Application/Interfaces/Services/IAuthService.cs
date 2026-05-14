using Bachelor_s_Point.Application.DTOs;
using Bachelor_s_Point.Models;

namespace Bachelor_s_Point.Application.Interfaces.Services
{
    public interface IAuthService
    {
        /// <summary>
        /// Step 1: Validate input, store pending registration with OTP, and send OTP email.
        /// Does NOT create the user. Returns "Success" or error message.
        /// </summary>
        Task<string> StartRegistrationAsync(RegisterDto dto);

        /// <summary>
        /// Step 2: Verify the OTP, create the actual user, send confirmation email.
        /// </summary>
        Task<string> VerifyOtpAndCreateUserAsync(string email, string otp);

        /// <summary>Regenerate a fresh OTP for a pending registration and email it.</summary>
        Task<string> ResendOtpAsync(string email);

        Task<User?> LoginAsync(LoginDto dto);
    }
}