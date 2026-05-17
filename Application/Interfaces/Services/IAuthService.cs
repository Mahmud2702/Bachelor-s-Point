using Bachelor_s_Point.Application.DTOs;
using Bachelor_s_Point.Models;

namespace Bachelor_s_Point.Application.Interfaces.Services
{
    public interface IAuthService
    {
        Task<string> StartRegistrationAsync(RegisterDto dto);
        Task<string> VerifyOtpAndCreateUserAsync(string email, string otp);
        Task<string> ResendOtpAsync(string email);
        Task<User?> LoginAsync(LoginDto dto);

        /// <summary>Step 1 of forgot password: send OTP if email exists.</summary>
        Task<string> StartPasswordResetAsync(string email);

        /// <summary>Step 2 of forgot password: verify OTP and set new password.</summary>
        Task<string> ResetPasswordAsync(ResetPasswordDto dto);

        /// <summary>Resend OTP for password reset.</summary>
        Task<string> ResendPasswordResetOtpAsync(string email);
    }
}
