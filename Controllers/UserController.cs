using Microsoft.AspNetCore.Mvc;
using TodoApi;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.EntityFrameworkCore;


[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public UserController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost("signup")]
    public async Task<IActionResult> SignUp(string name, string email, string password, string biometricToken, string role)
    {
        // Check if the user already exists
        if (_context.Users.Any(u => u.Email == email))
        {
            return BadRequest("User with the given email already exists.");
        }

        // Hash the password
        var passwordHash = HashPassword(password);

        // Create the user object
        var user = new User
        {
            Name = name,
            Email = email,
            PasswordHash = passwordHash,
            BiometricToken = biometricToken,
            Role = role,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(string email, string biometricToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user == null)
        {
            return NotFound("User not found.");
        }

        if (!IsBiometricTokenValid(biometricToken, user.Id))
        {
            return Unauthorized("Invalid biometric authentication.");
        }

        // Here, perform the actions upon successful biometric login
        // For example, generate and return a JWT token for the authenticated session

        return Ok(user); // Or return an appropriate response
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<User>> GetUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound();
        }
        return user;
    }

    private bool IsBiometricTokenValid(string token, int userId)
    {
        // Example: Validate JWT token
        // var valid = ValidateJwtToken(token);
        // return valid;

        // Example: Validate a simple token
        var user = _context.Users.FirstOrDefault(u => u.Id == userId);
        if (user != null)
        {
            return user.BiometricToken == token;
        }
        return false;
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
}
