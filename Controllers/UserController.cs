using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using dotnet_user.Dtos.User;
using dotnet_user.Helpers;
using dotnet_user.Models;
using dotnet_user.Services.UserService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace dotnet_user.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class UserController : BaseApiController
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAllUsers([FromQuery] UserQueryDto query)
        {
            try
            {
                PagedResponse<List<GetUserDto>> users = await _userService.GetAllUsers(query);
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
            if (!ValidationHelpers.IsValidId(id))
            {
                return BadRequest(new { message = "Invalid id." });
            }
            try
            {
                GetUserDto user = await _userService.GetUserById(id);
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
            try
            {
                GetUserDto user = await _userService.AddUser(newUser);
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
            try
            {
                GetUserDto user = await _userService.UpdateUser(updatedUser);
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
            if (!ValidationHelpers.IsValidId(id))
            {
                return BadRequest(new { message = "Invalid id." });
            }

            try
            {
                string message = await _userService.DeleteUser(id);
                return OkResponse(message);
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex);
            }
        }
    }
}
