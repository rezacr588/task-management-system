using System;
using Microsoft.AspNetCore.Identity;
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
        private readonly ITokenGenerator _tokenGenerator;
        private readonly IPasswordHasher<User> _passwordHasher;

        public UserService(
            IUserRepository userRepository,
            IMapper mapper,
            ITokenGenerator tokenGenerator,
            IPasswordHasher<User> passwordHasher)
        {
            _userRepository = userRepository;
            _mapper = mapper;
            _tokenGenerator = tokenGenerator;
            _passwordHasher = passwordHasher;
        }

        public async Task<UserDto> CreateUserAsync(UserRegistrationDto registrationModel)
        {
            if (await UserExists(registrationModel.Email))
            {
                throw new InvalidOperationException("User with the given email already exists.");
            }

            var user = new User
            {
                Name = registrationModel.Name,
                Email = registrationModel.Email,
                PasswordHash = string.Empty,
                BiometricToken = registrationModel.BiometricToken,
                Role = registrationModel.Role,
                CreatedAt = DateTime.UtcNow
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, registrationModel.Password);

            await _userRepository.AddAsync(user);
            return _mapper.Map<UserDto>(user); // Return a DTO instead of the entity
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

            if (!string.IsNullOrWhiteSpace(updateModel.Password))
            {
                user.PasswordHash = _passwordHasher.HashPassword(user, updateModel.Password);
            }

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

        public async Task<LoginResponseDto> LoginAsync(LoginDto loginDto)
        {
            var user = await _userRepository.GetByEmailAsync(loginDto.Email);
            if (user == null)
            {
                throw new UnauthorizedAccessException("Invalid email or password.");
            }

            var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, loginDto.Password);
            if (verificationResult == PasswordVerificationResult.Failed)
            {
                throw new UnauthorizedAccessException("Invalid email or password.");
            }

            var token = _tokenGenerator.GenerateToken(user);
            var userDto = _mapper.Map<UserDto>(user);

            return new LoginResponseDto
            {
                Token = token,
                User = userDto,
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            };
        }
    }
}
