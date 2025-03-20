using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RepositoryLayer.Model;

namespace RepositoryLayer.Interface
{
    public interface IAddressBookRL
    {
        Task<IEnumerable<AddressBookEntry>> GetAllAsync(int? userId);
        Task<AddressBookEntry?> GetByIdAsync(int? userId, int id);
        Task AddAsync(AddressBookEntry entry);
        Task UpdateAsync(AddressBookEntry entry);
        Task DeleteAsync(int? userId, int id);
    }
}
