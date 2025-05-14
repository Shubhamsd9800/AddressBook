using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RepositoryLayer.DTO;
using RepositoryLayer.Model;

namespace RepositoryLayer.Interface
{
    public interface IAddressBookBL
    {
        Task<IEnumerable<AddressBookEntry>> GetAllAsync(int? userId);
        Task<AddressBookEntry?> GetByIdAsync(int? userId, int id);
        Task AddAsync(int userId, AddressBookDto dto);
        Task UpdateAsync(int? userId, int id, AddressBookDto dto);
        Task DeleteAsync(int? userId, int id);
    }
}
