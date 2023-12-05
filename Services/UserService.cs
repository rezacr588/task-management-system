using TodoApi;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.EntityFrameworkCore;

public class UserService {
    private readonly ApplicationDbContext _context;
    public UserService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<User> CreateUserAsync(UserRegistrationModel registrationModel)
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

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return user;
    }

    public async Task<bool> UserExists(string email)
      {
          return await _context.Users.AnyAsync(u => u.Email == email);
      }

    private string HashPassword(string password)
    {
        // Implement password hashing
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
        return await _context.Users.FindAsync(id);
    }


    public async Task<User> GetUserByEmailAsync(string email)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
    }
}