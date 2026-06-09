using Karavul.Core.Entities;
using Karavul.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Karavul.Services;

public class AuthService
{
    private readonly IUserRepository _userRepo;
    private readonly ILogger<AuthService> _logger;

    public AuthService(IUserRepository userRepo, ILogger<AuthService> logger)
    {
        _userRepo = userRepo;
        _logger = logger;
    }

    public async Task<User?> GetUserByIdAsync(string id) => await _userRepo.GetByIdAsync(id);

    public async Task<User?> ValidateLoginAsync(string username, string password)
    {
        var user = await _userRepo.GetByUsernameAsync(username);
        if (user == null)
        {
            _logger.LogWarning("Geçersiz kullanıcı adı ile giriş denemesi: {Username}", username);
            return null;
        }

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            _logger.LogWarning("Yanlış şifre ile giriş denemesi: {Username}", username);
            return null;
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _userRepo.UpdateAsync(user);

        _logger.LogInformation("Kullanıcı giriş yaptı: {Username}", username);
        return user;
    }

    public async Task<bool> ChangePasswordAsync(string userId, string newPassword)
    {
        var user = await _userRepo.GetByIdAsync(userId);
        if (user == null) return false;

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.IsPasswordChangeRequired = false;
        await _userRepo.UpdateAsync(user);

        _logger.LogInformation("Kullanıcı şifresini değiştirdi: {UserId}", userId);
        return true;
    }

    public async Task<IEnumerable<User>> GetAllUsersAsync() => await _userRepo.GetAllAsync();

    public async Task<string> CreateUserAsync(string username, string password, Karavul.Core.Enums.UserRole role, string? createdBy = null)
    {
        var user = new User
        {
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = role,
            IsPasswordChangeRequired = true,
            CreatedBy = createdBy,
            UpdatedBy = createdBy
        };
        var id = await _userRepo.CreateAsync(user);
        _logger.LogInformation("Yeni kullanıcı oluşturuldu: {Username}", username);
        return id;
    }

    public async Task<bool> UpdateUserRoleAsync(string userId, Karavul.Core.Enums.UserRole role, string? updatedBy = null)
    {
        var user = await _userRepo.GetByIdAsync(userId);
        if (user == null) return false;

        user.Role = role;
        user.UpdatedBy = updatedBy;
        await _userRepo.UpdateAsync(user);
        _logger.LogInformation("Kullanıcı rolü güncellendi: {UserId}", userId);
        return true;
    }

    public async Task DeleteUserAsync(string userId)
    {
        await _userRepo.DeleteAsync(userId);
        _logger.LogInformation("Kullanıcı silindi: {UserId}", userId);
    }

    public async Task SeedDefaultUserAsync()
    {
        if (await _userRepo.AnyAsync()) return;

        var defaultUser = new User
        {
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin"),
            IsPasswordChangeRequired = true,
            CreatedAt = DateTime.UtcNow
        };

        await _userRepo.CreateAsync(defaultUser);
        _logger.LogInformation("Varsayılan admin kullanıcısı oluşturuldu.");
    }
}
