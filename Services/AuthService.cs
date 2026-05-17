using Bachelor_s_Point.Application.DTOs;
using Bachelor_s_Point.Application.Interfaces.Services;
using Bachelor_s_Point.Application.Interfaces.UnitOfWork;
using Bachelor_s_Point.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Cryptography;
using System.Text;

namespace Bachelor_s_Point.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;
        private readonly ILogger<AuthService> _logger;
        private readonly PasswordHasher<User> _passwordHasher;

        private const int OtpValidityMinutes = 5;

        public AuthService(
            IUnitOfWork unitOfWork,
            IEmailService emailService,
            ILogger<AuthService> logger)
        {
            _unitOfWork = unitOfWork;
            _emailService = emailService;
            _logger = logger;
            _passwordHasher = new PasswordHasher<User>();
        }

        public async Task<string> StartRegistrationAsync(RegisterDto dto)
        {
            if (dto == null) return "Invalid registration data";
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
                return "Email and password are required";

            // Validate role exists
            var role = await _unitOfWork.RoleRepo.GetByIdAsync(dto.RoleId);
            if (role == null) return "Selected role does not exist";

            // Email must not already belong to a real user
            var existingUser = await _unitOfWork.UserRepo.GetUserByEmailAsync(dto.Email);
            if (existingUser != null) return "An account with this email already exists";

            // Generate secure 6-digit OTP
            string plainOtp = GenerateOtp();
            string otpHash = HashOtp(dto.Email, plainOtp);

            // Hash the password now — never store plain text, even temporarily
            string passwordHash = _passwordHasher.HashPassword(new User(), dto.Password);

            // Insert or update pending registration for this email
            var pending = await _unitOfWork.PendingRegRepo.GetByEmailAsync(dto.Email);
            if (pending == null)
            {
                pending = new PendingRegistration
                {
                    FullName = dto.FullName,
                    UserName = dto.UserName ?? string.Empty,
                    DateOfBirth = dto.DateOfBirth,
                    Email = dto.Email,
                    PhoneNumber = dto.PhoneNumber,
                    Address = dto.Address,
                    PasswordHash = passwordHash,
                    TargetRoleId = dto.RoleId,
                    OtpHash = otpHash,
                    OtpExpiresAt = DateTime.Now.AddMinutes(OtpValidityMinutes),
                    CreatedAt = DateTime.Now,
                    AttemptCount = 0
                };
                await _unitOfWork.PendingRegRepo.AddAsync(pending);
            }
            else
            {
                pending.FullName = dto.FullName;
                pending.UserName = dto.UserName ?? string.Empty;
                pending.DateOfBirth = dto.DateOfBirth;
                pending.PhoneNumber = dto.PhoneNumber;
                pending.Address = dto.Address;
                pending.PasswordHash = passwordHash;
                pending.TargetRoleId = dto.RoleId;
                pending.OtpHash = otpHash;
                pending.OtpExpiresAt = DateTime.Now.AddMinutes(OtpValidityMinutes);
                pending.AttemptCount = 0;
                _unitOfWork.PendingRegRepo.Update(pending);
            }

            await _unitOfWork.SaveAsync();

            // Send the OTP email
            try
            {
                await _emailService.SendOtpEmailAsync(dto.Email, dto.FullName ?? dto.UserName ?? "there", plainOtp, OtpValidityMinutes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send OTP email to {Email}", dto.Email);
                // Surface the underlying error so the developer can see what's wrong
                string innerMsg = ex.InnerException?.Message ?? ex.Message;
                return $"Email send failed: {innerMsg}";
            }

            return "Success";
        }

        public async Task<string> VerifyOtpAndCreateUserAsync(string email, string otp)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(otp))
                return "Email and OTP are required";

            var pending = await _unitOfWork.PendingRegRepo.GetByEmailAsync(email);
            if (pending == null)
                return "No pending registration found. Please register again.";

            if (DateTime.Now > pending.OtpExpiresAt)
                return "OTP has expired. Please request a new one.";

            string expectedHash = HashOtp(pending.Email, otp);
            if (!CryptographicEquals(expectedHash, pending.OtpHash))
            {
                pending.AttemptCount += 1;
                _unitOfWork.PendingRegRepo.Update(pending);
                await _unitOfWork.SaveAsync();
                return "Incorrect OTP. Please check your email and try again.";
            }

            // Race-condition safety: re-check that no user with this email exists
            var existingUser = await _unitOfWork.UserRepo.GetUserByEmailAsync(pending.Email);
            if (existingUser != null)
            {
                _unitOfWork.PendingRegRepo.Delete(pending);
                await _unitOfWork.SaveAsync();
                return "An account with this email already exists";
            }

            // Create the actual user
            var user = new User
            {
                FullName = pending.FullName,
                UserName = pending.UserName,
                DateOfBirth = pending.DateOfBirth,
                Email = pending.Email,
                PhoneNumber = pending.PhoneNumber,
                Address = pending.Address,
                PasswordHash = pending.PasswordHash,
                RoleId = pending.TargetRoleId,
                LastLogin = null
            };
            await _unitOfWork.UserRepo.AddAsync(user);

            // Clean up pending row
            _unitOfWork.PendingRegRepo.Delete(pending);

            await _unitOfWork.SaveAsync();

            // Send confirmation email (don't fail registration if this throws)
            try
            {
                await _emailService.SendRegistrationConfirmationAsync(user.Email!, user.FullName ?? user.UserName ?? "there");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send confirmation email to {Email}", user.Email);
            }

            return "Success";
        }

        public async Task<string> ResendOtpAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return "Email is required";

            var pending = await _unitOfWork.PendingRegRepo.GetByEmailAsync(email);
            if (pending == null)
                return "No pending registration found. Please register again.";

            string plainOtp = GenerateOtp();
            pending.OtpHash = HashOtp(pending.Email, plainOtp);
            pending.OtpExpiresAt = DateTime.Now.AddMinutes(OtpValidityMinutes);
            pending.AttemptCount = 0;
            _unitOfWork.PendingRegRepo.Update(pending);
            await _unitOfWork.SaveAsync();

            try
            {
                await _emailService.SendOtpEmailAsync(pending.Email, pending.FullName ?? pending.UserName, plainOtp, OtpValidityMinutes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to resend OTP to {Email}", email);
                return "Could not send verification email. Please try again.";
            }

            return "Success";
        }

        public async Task<User?> LoginAsync(LoginDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
                return null;

            var user = await _unitOfWork.UserRepo.GetUserByEmailAsync(dto.Email);
            if (user == null || string.IsNullOrEmpty(user.PasswordHash)) return null;

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
            if (result == PasswordVerificationResult.Failed) return null;

            user.LastLogin = DateTime.Now;
            _unitOfWork.UserRepo.Update(user);
            await _unitOfWork.SaveAsync();
            return user;
        }


        // ============================================================
        // FORGOT PASSWORD FLOW
        // ============================================================

        public async Task<string> StartPasswordResetAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return "Email is required";

            var user = await _unitOfWork.UserRepo.GetUserByEmailAsync(email);
            if (user == null)
            {
                // For security: don't reveal whether email exists. Return Success anyway
                // so attackers can't enumerate registered emails. Just don't send the OTP.
                return "Success";
            }

            string plainOtp = GenerateOtp();
            string otpHash = HashOtp(email, plainOtp);

            // Upsert reset token for this email
            var existing = await _unitOfWork.PasswordResetRepo.GetByEmailAsync(email);
            if (existing == null)
            {
                var token = new PasswordResetToken
                {
                    Email = email,
                    OtpHash = otpHash,
                    OtpExpiresAt = DateTime.Now.AddMinutes(OtpValidityMinutes),
                    CreatedAt = DateTime.Now,
                    AttemptCount = 0
                };
                await _unitOfWork.PasswordResetRepo.AddAsync(token);
            }
            else
            {
                existing.OtpHash = otpHash;
                existing.OtpExpiresAt = DateTime.Now.AddMinutes(OtpValidityMinutes);
                existing.AttemptCount = 0;
                _unitOfWork.PasswordResetRepo.Update(existing);
            }
            await _unitOfWork.SaveAsync();

            try
            {
                await _emailService.SendPasswordResetOtpAsync(email, user.FullName ?? user.UserName, plainOtp, OtpValidityMinutes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset OTP to {Email}", email);
                string innerMsg = ex.InnerException?.Message ?? ex.Message;
                return $"Email send failed: {innerMsg}";
            }

            return "Success";
        }

        public async Task<string> ResetPasswordAsync(ResetPasswordDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Email) ||
                string.IsNullOrWhiteSpace(dto.Otp) || string.IsNullOrWhiteSpace(dto.NewPassword))
                return "All fields are required";

            var token = await _unitOfWork.PasswordResetRepo.GetByEmailAsync(dto.Email);
            if (token == null)
                return "No password reset request found. Please start again.";

            if (DateTime.Now > token.OtpExpiresAt)
                return "OTP has expired. Please request a new one.";

            string expectedHash = HashOtp(token.Email, dto.Otp);
            if (!CryptographicEquals(expectedHash, token.OtpHash))
            {
                token.AttemptCount += 1;
                _unitOfWork.PasswordResetRepo.Update(token);
                await _unitOfWork.SaveAsync();
                return "Incorrect OTP. Please check your email and try again.";
            }

            var user = await _unitOfWork.UserRepo.GetUserByEmailAsync(token.Email);
            if (user == null)
            {
                _unitOfWork.PasswordResetRepo.Delete(token);
                await _unitOfWork.SaveAsync();
                return "User account no longer exists.";
            }

            // Hash and save the new password
            user.PasswordHash = _passwordHasher.HashPassword(user, dto.NewPassword);
            _unitOfWork.UserRepo.Update(user);

            // Consume the token
            _unitOfWork.PasswordResetRepo.Delete(token);

            await _unitOfWork.SaveAsync();

            // Send confirmation (don't fail reset if email errors)
            try
            {
                await _emailService.SendPasswordChangedConfirmationAsync(user.Email!, user.FullName ?? user.UserName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send password-changed confirmation to {Email}", user.Email);
            }

            return "Success";
        }

        public async Task<string> ResendPasswordResetOtpAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return "Email is required";

            var token = await _unitOfWork.PasswordResetRepo.GetByEmailAsync(email);
            if (token == null)
                return "No password reset request found. Please start again.";

            var user = await _unitOfWork.UserRepo.GetUserByEmailAsync(email);
            if (user == null)
            {
                _unitOfWork.PasswordResetRepo.Delete(token);
                await _unitOfWork.SaveAsync();
                return "User account no longer exists.";
            }

            string plainOtp = GenerateOtp();
            token.OtpHash = HashOtp(email, plainOtp);
            token.OtpExpiresAt = DateTime.Now.AddMinutes(OtpValidityMinutes);
            token.AttemptCount = 0;
            _unitOfWork.PasswordResetRepo.Update(token);
            await _unitOfWork.SaveAsync();

            try
            {
                await _emailService.SendPasswordResetOtpAsync(email, user.FullName ?? user.UserName, plainOtp, OtpValidityMinutes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to resend password reset OTP to {Email}", email);
                string innerMsg = ex.InnerException?.Message ?? ex.Message;
                return $"Email send failed: {innerMsg}";
            }

            return "Success";
        }

        // ---------- helpers ----------

        private static string GenerateOtp()
        {
            // Cryptographically secure 6-digit OTP in [100000, 999999]
            byte[] bytes = RandomNumberGenerator.GetBytes(4);
            uint num = BitConverter.ToUInt32(bytes, 0);
            int otp = (int)(100000 + (num % 900000));
            return otp.ToString();
        }

        private static string HashOtp(string email, string otp)
        {
            using var sha = SHA256.Create();
            byte[] data = Encoding.UTF8.GetBytes($"{email.ToLowerInvariant()}:{otp}");
            return Convert.ToBase64String(sha.ComputeHash(data));
        }

        /// <summary>Constant-time comparison to avoid timing attacks.</summary>
        private static bool CryptographicEquals(string a, string b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}
