using Bachelor_s_Point.Application.DTOs;
using Bachelor_s_Point.Models;

namespace Bachelor_s_Point.Application.Interfaces.Services
{
    public interface IAuthService
    {
        Task<string> StartRegistrationAsync(RegisterDto dto);

        /// <summary>
        /// Verifies OTP and creates the user.
        /// Returns ("Success", createdUser) on success, or (errorMessage, null) on failure.
        /// </summary>
        Task<(string Result, User? User)> VerifyOtpAndCreateUserAsync(string email, string otp);

        Task<string> ResendOtpAsync(string email);
        Task<User?> LoginAsync(LoginDto dto);

        /// <summary>
        /// Checks the separate Admin table. Returns the Admin if credentials match, null otherwise.
        /// </summary>
        Task<Admin?> LoginAdminAsync(LoginDto dto);

        Task<string> StartPasswordResetAsync(string email);
        Task<string> ResetPasswordAsync(ResetPasswordDto dto);
        Task<string> ResendPasswordResetOtpAsync(string email);
        Task<string> ChangePasswordAsync(int userId, ChangePasswordDto dto);
    }
}
