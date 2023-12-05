using Microsoft.AspNetCore.Mvc;
using TodoApi;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;

public class UserRegistrationModel
{
    [Required]
    [StringLength(100)]
    public required string Name { get; set; }

    [Required]
    [EmailAddress]
    public required string Email { get; set; }

    [Required]
    [MinLength(6)] // Example: Minimum length for password
    public required string Password { get; set; }

    // Depending on your requirements, you might want to validate the biometricToken and role as well
    public required string BiometricToken { get; set; }

    [Required]
    public required string Role { get; set; }
}


[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;


    public UserController(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;        
        _configuration = configuration;
    }

    [HttpPost("signup")]
    public async Task<IActionResult> SignUp([FromBody] UserRegistrationModel registrationModel)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (_context.Users.Any(u => u.Email == registrationModel.Email))
        {
            return BadRequest("User with the given email already exists.");
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

        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
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

        var tokenString = GenerateJwtToken(user);
        return Ok(new { Token = tokenString });
    }

    [Authorize]
    [HttpGet("profile")]
    public async Task<IActionResult> GetUserProfile()
    {
        // Extract the email claim from the JWT token
        var email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

        if (email == null)
        {
            return Unauthorized();
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user == null)
        {
            return NotFound("User not found.");
        }

        return Ok(user);
    }

    private string GenerateJwtToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Email),
            new Claim("id", user.Id.ToString()),
            // Add additional claims as needed
        };

        var token = new JwtSecurityToken(
            _configuration["Jwt:Issuer"],
            _configuration["Jwt:Audience"],
            claims,
            expires: DateTime.Now.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
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
