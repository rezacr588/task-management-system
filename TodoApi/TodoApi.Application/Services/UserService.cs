using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using TodoApi.Application.DTOs;
using TodoApi.Domain.Entities;
using TodoApi.Domain.Interfaces;

namespace TodoApi.Application.Services
{
    public class UserService
    {
        private readonly IRepository<User> _userRepository;

        public UserService(IRepository<User> userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<User> CreateUserAsync(UserRegistrationDto registrationModel)
        {
            if (await UserExists(registrationModel.Email))
            {
                throw new Exception("User with the given email already exists.");
            }

            var passwordHash = HashPassword(registrationModel.Password);

            var user = new User
            {
                Name = registrationModel.Name,
                Email = registrationModel.Email,
                PasswordHash = passwordHash,
                BiometricToken = registrationModel.BiometricToken,
                Role = registrationModel.Role,
                CreatedAt = DateTime.UtcNow
            };

            await _userRepository.AddAsync(user);
            return user;
        }

        private string HashPassword(string password)
        {
            // Password hashing logic remains the same
            byte[] salt = new byte[128 / 8];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            return Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 100000,
                numBytesRequested: 256 / 8));
        }

        public async Task<User> GetUserByIdAsync(int id)
        {
            return await _userRepository.GetByIdAsync(id);
        }

        public async Task<bool> UserExists(string email)
        {
            var users = await _userRepository.FindAsync(u => u.Email == email);
            return users.Any();
        }
        public async Task<User> GetUserByEmailAsync(string email)
        {
            // Await the completion of FindAsync to get IEnumerable<User>,
            // then apply FirstOrDefaultAsync
            var users = await _userRepository.FindAsync(u => u.Email == email);
            return users.FirstOrDefault();
        }
    }
}
