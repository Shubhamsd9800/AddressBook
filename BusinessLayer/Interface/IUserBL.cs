using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RepositoryLayer.DTO;
using RepositoryLayer.Model;

namespace BusinessLayer.Interface
{
    public interface IUserBL
    {
        Task<ApiResponse<string>> RegisterUserAsync(UserDto userDto);
        Task<ApiResponse<string>> LoginUserAsync(LoginDto loginDto);
        Task<User?> GetByEmailAsync(string email);
        Task UpdatePasswordAsync(int userId, string newPassword);
    }
}
