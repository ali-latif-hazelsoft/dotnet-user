using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using dotnet_user.Dtos.ApplicationUser;
using dotnet_user.Models;
using dotnet_user.Services.Email;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace dotnet_user.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration,
            IEmailSender emailSender,
            ILogger<AuthController> logger
        )
        {
            _userManager = userManager;
            _configuration = configuration;
            _emailSender = emailSender;
            _logger = logger;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto request)
        {
            try
            {
                if (
                    string.IsNullOrWhiteSpace(request.Username)
                    || string.IsNullOrWhiteSpace(request.Password)
                )
                    return BadRequest("Username and password are required.");

                var user = await _userManager.FindByNameAsync(request.Username);
                if (user == null)
                {
                    user = await _userManager.FindByEmailAsync(request.Username);
                }

                if (user == null)
                    return Unauthorized("Invalid username or password.");

                if (!user.EmailConfirmed)
                    return Unauthorized("Please confirm your email and set your password first.");

                var validPassword = await _userManager.CheckPasswordAsync(user, request.Password);
                if (!validPassword)
                    return Unauthorized("Invalid username or password.");

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
                    new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                };

                var tokenKey = _configuration["AppSettings:Token"];
                if (string.IsNullOrWhiteSpace(tokenKey))
                    return StatusCode(500, "JWT token key is missing.");

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenKey));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

                var token = new JwtSecurityToken(
                    claims: claims,
                    expires: DateTime.UtcNow.AddDays(1),
                    signingCredentials: creds
                );

                var jwt = new JwtSecurityTokenHandler().WriteToken(token);

                return Ok(new { token = jwt, user = new { user.UserName, user.Email } });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while logging in.");
                return StatusCode(500, "Something went wrong while logging in.");
            }
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPost("registerNewUser")]
        public async Task<IActionResult> InviteUser([FromBody] InviteUserDto request)
        {
            Console.WriteLine("Registering Email");
            try
            {
                Console.WriteLine("For Email" + request.Email);
                if (string.IsNullOrWhiteSpace(request.Email))
                    return BadRequest("Email is required.");

                string email = request.Email.Trim().ToLowerInvariant();
                string username = string.IsNullOrWhiteSpace(request.Username)
                    ? email
                    : request.Username.Trim();

                var existingUserByEmail = await _userManager.FindByEmailAsync(email);
                Console.WriteLine("existingUserByEmail:" + existingUserByEmail);
                Console.WriteLine($"Is user null? {existingUserByEmail == null}");

                var existingUserByName = await _userManager.FindByNameAsync(username);
                if (existingUserByName != null)
                    return BadRequest("A user with this username already exists.");

                var user = new ApplicationUser
                {
                    UserName = username,
                    Email = email,
                    EmailConfirmed = false,
                };

                var createResult = await _userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                    return BadRequest(createResult.Errors);

                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
                Console.WriteLine("encodedToken: " + encodedToken);

                var confirmUrl =
                    $"{Request.Scheme}://{Request.Host}/auth/confirmEmailAndSetPassword?userId={user.Id}&token={encodedToken}";

                var subject = "Confirm your account and set password";
                var message =
                    $@"
                    <p>Hello {WebUtility.HtmlEncode(username)},</p>
                    <p>Please click the link below to confirm your email and set your password:</p>
                    <p><a href=""{confirmUrl}"">Confirm Email / Set Password</a></p>
                    <p>If you did not request this, you can ignore this email.</p>";

                await _emailSender.SendEmailAsync(email, subject, message);

                return Ok(
                    new
                    {
                        message = "User invited successfully. Confirmation email sent.",
                        userId = user.Id,
                        email = user.Email,
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while inviting user.");
                return StatusCode(500, "Something went wrong while inviting user.");
            }
        }

        [AllowAnonymous]
        [HttpPost("confirmEmailAndSetPassword")]
        public async Task<IActionResult> SetPassword([FromForm] SetPasswordDto request)
        {
            try
            {
                if (
                    string.IsNullOrWhiteSpace(request.UserId)
                    || string.IsNullOrWhiteSpace(request.Token)
                    || string.IsNullOrWhiteSpace(request.Password)
                    || string.IsNullOrWhiteSpace(request.ConfirmPassword)
                )
                {
                    return BadRequest("All fields are required.");
                }

                if (request.Password != request.ConfirmPassword)
                    return BadRequest("Password and confirm password do not match.");

                var user = await _userManager.FindByIdAsync(request.UserId);
                if (user == null)
                    return NotFound("User not found.");

                if (user.EmailConfirmed)
                    return BadRequest("Email is already confirmed.");

                var decodedToken = Encoding.UTF8.GetString(
                    WebEncoders.Base64UrlDecode(request.Token)
                );

                var confirmResult = await _userManager.ConfirmEmailAsync(user, decodedToken);
                if (!confirmResult.Succeeded)
                    return BadRequest(confirmResult.Errors);

                var passwordResult = await _userManager.AddPasswordAsync(user, request.Password);
                if (!passwordResult.Succeeded)
                    return BadRequest(passwordResult.Errors);

                return Ok(
                    new
                    {
                        message = "Email confirmed and password set successfully. You can now log in.",
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while setting password.");
                return StatusCode(500, "Something went wrong while setting password.");
            }
        }
    }
}
