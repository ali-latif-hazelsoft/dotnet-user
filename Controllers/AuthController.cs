using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using dotnet_user.Dtos.ApplicationUser;
using dotnet_user.Dtos.Auth;
using dotnet_user.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace dotnet_user.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto request)
        {
            try
            {
                LoginResponseDto response = await _authService.LoginAsync(request);
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while logging in.");
                return StatusCode(500, new { message = "Something went wrong while logging in." });
            }
        }

        [Authorize]
        [HttpPost("registerNewUser")]
        public async Task<IActionResult> InviteUser([FromBody] InviteUserDto request)
        {
            try
            {
                InviteUserResponseDto response = await _authService.InviteUserAsync(request);
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while inviting user.");
                return StatusCode(
                    500,
                    new { message = "Something went wrong while inviting user." }
                );
            }
        }

        [AllowAnonymous]
        [HttpPost("confirmEmailAndSetPassword")]
        public async Task<IActionResult> ConfirmEmailAndSetPassword(
            [FromQuery] SetPasswordDto request
        )
        {
            if (request.Password != request.ConfirmPassword)
            {
                throw new ArgumentException("Password and confirm password do not match.");
            }
            try
            {
                ConfirmEmailResponseDto response =
                    await _authService.ConfirmEmailAndSetPasswordAsync(request);

                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while setting password.");
                return StatusCode(
                    500,
                    new { message = "Something went wrong while setting password." }
                );
            }
        }
    }
}
