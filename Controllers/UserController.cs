using System;
using System.Threading.Tasks;
using dotnet_user.Dtos.User;
using dotnet_user.Services.UserService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace dotnet_user.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        private IActionResult HandleServiceError(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return BadRequest();

            if (message.Contains("not found", StringComparison.OrdinalIgnoreCase))
                return NotFound(new { success = false, message });

            if (message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
                return Conflict(new { success = false, message });

            if (message.Contains("required", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { success = false, message });

            return BadRequest(new { success = false, message });
        }

        [HttpGet]
        public async Task<IActionResult> GetAllUsers([FromQuery] UserQueryDto query)
        {
            try
            {
                var response = await _userService.GetAllUsers(query);

                if (!response.Success)
                    return HandleServiceError(response.Message);

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            try
            {
                var response = await _userService.GetUserById(id);

                if (!response.Success)
                    return HandleServiceError(response.Message);

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddUser([FromBody] AddUserDto newUser)
        {
            try
            {
                var response = await _userService.AddUser(newUser);

                if (!response.Success)
                    return HandleServiceError(response.Message);

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPut]
        public async Task<IActionResult> UpdateUser([FromBody] UpdateUserDto updatedUser)
        {
            try
            {
                var response = await _userService.UpdateUser(updatedUser);

                if (!response.Success)
                    return HandleServiceError(response.Message);

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                var response = await _userService.DeleteUser(id);

                if (!response.Success)
                    return HandleServiceError(response.Message);

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }
}
