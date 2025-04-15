using _8lpets.Models;

namespace _8lpets.Services
{
    public interface IUserService
    {
        Task<User?> GetUserByIdAsync(int id);
        Task<User?> GetUserByUsernameAsync(string username);
        Task<User?> GetUserByEmailAsync(string email);
        Task<User?> AuthenticateAsync(string username, string password);
        Task<User> RegisterUserAsync(string username, string email, string password);
        Task<bool> UpdateUserAsync(User user);
        string HashPassword(string password);
        bool VerifyPassword(string password, string passwordHash);
    }
}
