using System.Threading.Tasks;
using TodoApi.Domain.Entities;
using TodoApi.Application.DTOs;

namespace TodoApi.Application.Interfaces
{
    public interface IUserService
    {
        Task<User> CreateUserAsync(UserRegistrationDto registrationModel);
        Task<User> GetUserByIdAsync(int id);
        Task<User> GetUserByEmailAsync(string email);
        Task UpdateUserAsync(int id, UserUpdateDto updateModel);
        Task DeleteUserAsync(int id);
    }
}
