using Karavul.Core.Entities;

namespace Karavul.Core.Interfaces;

public interface IAppSettingRepository
{
    Task<string?> GetAsync(string key);
    Task SetAsync(string key, string value);
    Task<IEnumerable<AppSetting>> GetAllAsync();
}
