using Bachelor_s_Point.Application.DTOs;
using Bachelor_s_Point.Models;

namespace Bachelor_s_Point.Application.Interfaces.Services
{
    public interface IAuthService
    {
        Task<string> RegisterAsync(RegisterDto registerDto);

        Task<User?> LoginAsync(LoginDto loginDto);
    }
}
