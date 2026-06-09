using Karavul.Core.Entities;

namespace Karavul.Core.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByIdAsync(string id);
    Task<string> CreateAsync(User user);
    Task UpdateAsync(User user);
    Task<IEnumerable<User>> GetAllAsync();
    Task DeleteAsync(string id);
    Task<bool> AnyAsync();
}
