using Karavul.Core.Entities;

namespace Karavul.Core.Interfaces;

public interface IContactGroupRepository
{
    Task<IEnumerable<ContactGroup>> GetAllAsync();
    Task<ContactGroup?> GetByIdAsync(string id);
    Task<string> CreateAsync(ContactGroup group);
    Task UpdateAsync(ContactGroup group);
    Task DeleteAsync(string id);
}
