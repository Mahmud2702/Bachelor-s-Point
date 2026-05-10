using Bachelor_s_Point.Models;

namespace Bachelor_s_Point.Application.Interfaces.Repositories
{
    public interface IUserRepository : IBaseRepository<User>
    {
        Task<User?> GetUserByEmailAsync(string email);

        Task<List<User>> GetAllUsersWithRoleAsync();

        Task<User?> GetUserWithRoleByIdAsync(int id);
    }
}
