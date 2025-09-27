using System;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using TodoApi.Application.DTOs;
using TodoApi.Application.Interfaces;
using TodoApi.Domain.Entities;
using AutoMapper;

namespace TodoApi.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;

        public UserService(IUserRepository userRepository, IMapper mapper)
        {
            _userRepository = userRepository;
            _mapper = mapper;
        }

        public async Task<UserDto> CreateUserAsync(UserRegistrationDto registrationModel)
        {
            if (await UserExists(registrationModel.Email))
            {
                throw new InvalidOperationException("User with the given email already exists.");
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
            return _mapper.Map<UserDto>(user); // Return a DTO instead of the entity
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

        public async Task<UserDto> GetUserByIdAsync(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                throw new KeyNotFoundException("User not found.");
            }

            return _mapper.Map<UserDto>(user); // Return a DTO instead of the entity
        }

        public async Task<bool> UserExists(string email)
        {
            var users = await _userRepository.GetByEmailAsync(email);
            return users != null;
        }
        public async Task<UserDto> GetUserByEmailAsync(string email)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
            {
                throw new KeyNotFoundException("User not found.");
            }

            return _mapper.Map<UserDto>(user); // Return a DTO instead of the entity
        }

        public async Task UpdateUserAsync(int id, UserUpdateDto updateModel)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                throw new KeyNotFoundException("User not found.");
            }

            // Map the updated properties here
            user.Name = updateModel.Name;
            user.Email = updateModel.Email;
            user.BiometricToken = updateModel.BiometricToken;
            user.Role = updateModel.Role;

            await _userRepository.UpdateAsync(user);
        }

        public async Task DeleteUserAsync(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                throw new KeyNotFoundException("User not found.");
            }

            await _userRepository.DeleteAsync(user);
        }
    }
}
