using Bachelor_s_Point.Application.DTOs;
using Bachelor_s_Point.Application.Interfaces.Services;
using Bachelor_s_Point.Application.Interfaces.UnitOfWork;
using Bachelor_s_Point.Models;
using Microsoft.AspNetCore.Identity;

namespace Bachelor_s_Point.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly PasswordHasher<User> _passwordHasher;

        public AuthService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
            _passwordHasher = new PasswordHasher<User>();
        }

        public async Task<string> RegisterAsync(RegisterDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            {
                return "Invalid registration data";
            }

            var existing = await _unitOfWork.UserRepo.GetUserByEmailAsync(dto.Email);

            if (existing != null)
            {
                return "Email already exists";
            }

            var roleExists = await _unitOfWork.RoleRepo.GetByIdAsync(dto.RoleId);

            if (roleExists == null)
            {
                return "Selected role does not exist";
            }

            var user = new User
            {
                UserName = dto.UserName,
                Email = dto.Email,
                Address = dto.Address,
                RoleId = dto.RoleId,
                LastLogin = null
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);

            await _unitOfWork.UserRepo.AddAsync(user);
            await _unitOfWork.SaveAsync();

            return "Success";
        }

        public async Task<User?> LoginAsync(LoginDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            {
                return null;
            }

            var user = await _unitOfWork.UserRepo.GetUserByEmailAsync(dto.Email);

            if (user == null || string.IsNullOrEmpty(user.PasswordHash))
            {
                return null;
            }

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);

            if (result == PasswordVerificationResult.Failed)
            {
                return null;
            }

            user.LastLogin = DateTime.Now;
            _unitOfWork.UserRepo.Update(user);
            await _unitOfWork.SaveAsync();

            return user;
        }
    }
}
