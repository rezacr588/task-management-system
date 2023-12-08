using System.Threading.Tasks;
using TodoApi.Domain.Entities;

namespace TodoApi.Application.Interfaces
{
    public interface IUserService
    {
        Task<User> CreateUserAsync(UserRegistrationModel registrationModel);
        Task<User> GetUserByIdAsync(int id);
        Task<User> GetUserByEmailAsync(string email);
        Task UpdateUserAsync(int id, UserUpdateModel updateModel);
        Task DeleteUserAsync(int id);
    }
}
