using Microsoft.AspNetCore.Mvc;
using TodoApi.Application.DTOs;
using TodoApi.Application.Interfaces;
// Include other necessary namespaces

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

        var result = await _userService.RegisterUserAsync(registrationDto);

        if (result.Success)
        {
            return Ok(result);
        }
        else
        {
            return BadRequest(result.Message);
        }
    }

    // PUT: api/User/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UserUpdateDto userUpdateDto)
    {
        if (id != userUpdateDto.Id)
        {
            return BadRequest("ID mismatch");
        }

        var result = await _userService.UpdateUserAsync(id, userUpdateDto);

        if (result.Success)
        {
            return Ok(result);
        }
        else
        {
            return BadRequest(result.Message);
        }
    }

    // DELETE: api/User/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var result = await _userService.DeleteUserAsync(id);

        if (result.Success)
        {
            return Ok(result);
        }
        else
        {
            return BadRequest(result.Message);
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

    // Additional actions can be added here
}
