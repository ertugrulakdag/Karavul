using Karavul.Core.Entities;

namespace Karavul.Core.Interfaces;

public interface IDirectoryContactRepository
{
    Task<IEnumerable<DirectoryContact>> GetAllAsync();
    Task<DirectoryContact?> GetByIdAsync(string id);
    Task CreateAsync(DirectoryContact contact);
    Task UpdateAsync(DirectoryContact contact);
    Task DeleteAsync(string id);
}
