using System;
using System.Threading.Tasks;
using dotnet_user.Dtos.User;
using dotnet_user.Services.UserService;
using Microsoft.AspNetCore.Mvc;

namespace dotnet_user.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserController : BaseApiController
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        private static bool IsValidId(int id)
        {
            return id > 0;
        }

        private static string ValidateAddUser(AddUserDto newUser)
        {
            if (newUser == null)
            {
                return "User data is required.";
            }

            if (string.IsNullOrWhiteSpace(newUser.FirstName))
            {
                return "First name is required.";
            }

            if (string.IsNullOrWhiteSpace(newUser.LastName))
            {
                return "Last name is required.";
            }

            if (string.IsNullOrWhiteSpace(newUser.Email))
            {
                return "Email is required.";
            }

            return null;
        }

        private static string ValidateUpdateUser(UpdateUserDto updatedUser)
        {
            if (updatedUser == null)
            {
                return "User data is required.";
            }

            if (!IsValidId(updatedUser.Id))
            {
                return "Invalid id.";
            }

            if (string.IsNullOrWhiteSpace(updatedUser.FirstName))
            {
                return "First name is required.";
            }

            if (string.IsNullOrWhiteSpace(updatedUser.LastName))
            {
                return "Last name is required.";
            }

            if (string.IsNullOrWhiteSpace(updatedUser.Email))
            {
                return "Email is required.";
            }

            return null;
        }

        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAllUsers([FromQuery] UserQueryDto query)
        {
            try
            {
                var users = await _userService.GetAllUsers(query);
                return OkResponse(users, "Users fetched successfully.");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex);
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            if (!IsValidId(id))
            {
                return BadRequest(new { message = "Invalid id." });
            }

            try
            {
                var user = await _userService.GetUserById(id);
                return OkResponse(user, "User fetched successfully.");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex);
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddUser([FromBody] AddUserDto newUser)
        {
            string validationError = ValidateAddUser(newUser);
            if (validationError != null)
            {
                return BadRequest(new { message = validationError });
            }

            try
            {
                var user = await _userService.AddUser(newUser);
                return OkResponse(user, "User created successfully.");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex);
            }
        }

        [HttpPut]
        public async Task<IActionResult> UpdateUser([FromBody] UpdateUserDto updatedUser)
        {
            string validationError = ValidateUpdateUser(updatedUser);
            if (validationError != null)
            {
                return BadRequest(new { message = validationError });
            }

            try
            {
                var user = await _userService.UpdateUser(updatedUser);
                return OkResponse(user, "User updated successfully.");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            if (!IsValidId(id))
            {
                return BadRequest(new { message = "Invalid id." });
            }

            try
            {
                var message = await _userService.DeleteUser(id);
                return OkResponse(message);
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex);
            }
        }
    }
}
