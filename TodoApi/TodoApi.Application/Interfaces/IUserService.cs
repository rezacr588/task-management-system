using System.Threading.Tasks;
using TodoApi.Domain.Entities;
using TodoApi.Application.DTOs;

namespace TodoApi.Application.Interfaces
{
    public interface IUserService
    {
        // Method to create a new user
        Task<User> CreateUserAsync(UserRegistrationDto registrationModel);

        // Method to retrieve a user by their ID
        Task<User> GetUserByIdAsync(int id);

        // Method to retrieve a user by their email
        Task<User> GetUserByEmailAsync(string email);

        // Method to update a user's details
        Task UpdateUserAsync(int id, UserUpdateDto updateModel);

        // Method to delete a user
        Task DeleteUserAsync(int id);

        // Additional methods can be declared here as needed
    }
}
