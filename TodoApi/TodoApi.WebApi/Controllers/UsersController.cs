using Microsoft.AspNetCore.Mvc;
using TodoApi.Application.DTOs;
using TodoApi.Application.Interfaces;
using System.Threading.Tasks;

[Route("api/[controller]")]
[ApiController]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    // POST: api/User/register
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] UserRegistrationDto registrationDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var user = await _userService.CreateUserAsync(registrationDto);
            return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, user);
        }
        catch (Exception ex)
        {
            // Handle specific exceptions if necessary and return appropriate HTTP response
            return BadRequest(ex.Message);
        }
    }

    // PUT: api/User/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UserUpdateDto userUpdateDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            await _userService.UpdateUserAsync(id, userUpdateDto);
            return NoContent(); // 204 No Content is often used for successful PUT requests
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            // Handle specific exceptions if necessary
            return BadRequest(ex.Message);
        }
    }

    // DELETE: api/User/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        try
        {
            await _userService.DeleteUserAsync(id);
            return NoContent(); // 204 No Content is often used for successful DELETE requests
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            // Handle specific exceptions if necessary
            return BadRequest(ex.Message);
        }
    }

    // GET: api/User/5
    [HttpGet("{id}")]
    public async Task<IActionResult> GetUserById(int id)
    {
        var user = await _userService.GetUserByIdAsync(id);

        if (user == null)
        {
            return NotFound();
        }

        return Ok(user);
    }

    // GET: api/User/ByEmail/{email}
    [HttpGet("ByEmail/{email}")]
    public async Task<IActionResult> GetUserByEmail(string email)
    {
        var user = await _userService.GetUserByEmailAsync(email);

        if (user == null)
        {
            return NotFound();
        }

        return Ok(user);
    }

    // Additional actions can be added here as needed
}
