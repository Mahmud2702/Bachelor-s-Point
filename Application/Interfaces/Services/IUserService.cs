using Bachelor_s_Point.Models;

namespace Bachelor_s_Point.Application.Interfaces.Services
{
    public interface IUserService
    {
        Task<List<User>> GetAllUsersAsync();

        Task<User?> GetUserByIdAsync(int id);

        Task<string> CreateUserAsync(User user);

        Task<string> UpdateUserAsync(User user);

        Task DeleteUserAsync(int id);
    }
}
