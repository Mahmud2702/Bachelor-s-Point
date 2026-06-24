using Bachelor_s_Point.Application.Interfaces.Services;
using Bachelor_s_Point.Application.Interfaces.UnitOfWork;
using Bachelor_s_Point.Models;
using Microsoft.AspNetCore.Identity;

namespace Bachelor_s_Point.Services
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly PasswordHasher<User> _passwordHasher;

        public UserService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
            _passwordHasher = new PasswordHasher<User>();
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _unitOfWork.UserRepo.GetAllUsersWithRoleAsync();
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            return await _unitOfWork.UserRepo.GetUserWithRoleByIdAsync(id);
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _unitOfWork.UserRepo.GetUserByEmailAsync(email);
        }

        public async Task<bool> UserNameExistsAsync(string userName)
        {
            var all = await _unitOfWork.UserRepo.GetAllAsync();
            return all.Any(u => u.UserName?.ToLower() == userName.ToLower());
        }

        public async Task<string> CreateUserAsync(User user)
        {
            var existing = await _unitOfWork.UserRepo.GetUserByEmailAsync(user.Email!);
            if (existing != null) return "Email already exists";

            string plainPassword = user.PasswordHash!;
            user.PasswordHash = _passwordHasher.HashPassword(user, plainPassword);
            user.LastLogin = null;

            await _unitOfWork.UserRepo.AddAsync(user);
            await _unitOfWork.SaveAsync();

            return "Success";
        }

        public async Task<string> UpdateUserAsync(User user)
        {
            var existing = await _unitOfWork.UserRepo.GetByIdAsync(user.Id);
            if (existing == null) return "User not found";

            existing.UserName = user.UserName;
            existing.Email = user.Email;
            existing.Address = user.Address;
            existing.RoleId = user.RoleId;

            if (!string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                existing.PasswordHash = _passwordHasher.HashPassword(existing, user.PasswordHash);
            }

            _unitOfWork.UserRepo.Update(existing);
            await _unitOfWork.SaveAsync();
            return "Success";
        }

        public async Task DeleteUserAsync(int id)
        {
            var user = await _unitOfWork.UserRepo.GetByIdAsync(id);
            if (user != null)
            {
                _unitOfWork.UserRepo.Delete(user);
                await _unitOfWork.SaveAsync();
            }
        }

        public async Task UpdateProfilePictureAsync(int userId, string? picturePath)
        {
            var user = await _unitOfWork.UserRepo.GetByIdAsync(userId);
            if (user == null) return;

            user.ProfilePicturePath = picturePath;
            _unitOfWork.UserRepo.Update(user);
            await _unitOfWork.SaveAsync();
        }
    }
}
