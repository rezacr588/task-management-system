using Microsoft.AspNetCore.Mvc;
using TodoApi;
using System.Security.Claims;
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
    private readonly UserService _userService;
    private readonly AuthorizationService _authService;

    public UserController(ApplicationDbContext context, IConfiguration configuration, UserService userService, AuthorizationService authService)
    {
        _context = context;
        _configuration = configuration;
        _userService = userService;
        _authService = authService;
    }

    [HttpPost("signup")]
    public async Task<IActionResult> SignUp([FromBody] UserRegistrationModel registrationModel)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var user = await _userService.CreateUserAsync(registrationModel);
            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
        }
        catch (Exception ex)
        {
            // This will catch exceptions thrown by the UserService, e.g., if the user already exists
            return BadRequest(ex.Message);
        }
    }


    [HttpGet("{id}")]
    public async Task<ActionResult<User>> GetUser(int id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }
        return user;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(string email, string biometricToken)
    {
        var user = await _userService.GetUserByEmailAsync(email);
        if (user == null)
        {
            return NotFound("User not found.");
        }

        if (!_authService.IsBiometricTokenValid(biometricToken, user.Id))
        {
            return Unauthorized("Invalid biometric authentication.");
        }

        var tokenString = _authService.GenerateJwtToken(user);
        return Ok(new { Token = tokenString });
    }

    [Authorize]
    [HttpGet("profile")]
    public async Task<IActionResult> GetUserProfile()
    {
        var email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

        if (email == null)
        {
            return Unauthorized();
        }

        var user = await _userService.GetUserByEmailAsync(email);

        if (user == null)
        {
            return NotFound("User not found.");
        }

        return Ok(user);
    }
}
