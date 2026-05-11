using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using dotnet_user.Dtos.ApplicationUser;
using dotnet_user.Dtos.Auth;
using dotnet_user.Helpers;
using dotnet_user.Models;
using dotnet_user.Services.Email;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace dotnet_user.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly IEmailSender _emailSender;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration,
            IEmailSender emailSender,
            IHttpContextAccessor httpContextAccessor
        )
        {
            _userManager = userManager;
            _configuration = configuration;
            _emailSender = emailSender;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<LoginResponseDto> LoginAsync(LoginDto request)
        {
            string usernameOrEmail = request.Username.Trim();
            string password = request.Password;

            ApplicationUser user = await _userManager.FindByNameAsync(usernameOrEmail);

            if (user == null)
            {
                user = await _userManager.FindByEmailAsync(usernameOrEmail);
            }

            if (user == null)
            {
                throw new UnauthorizedAccessException("Invalid username or password.");
            }

            if (!user.EmailConfirmed)
            {
                throw new UnauthorizedAccessException(
                    "Please confirm your email and set your password first."
                );
            }

            bool validPassword = await _userManager.CheckPasswordAsync(user, password);

            if (!validPassword)
            {
                throw new UnauthorizedAccessException("Invalid username or password.");
            }

            string jwt = AuthHelpers.GenerateJwtToken(user, _configuration);

            return new LoginResponseDto
            {
                Token = jwt,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
            };
        }

        public async Task<InviteUserResponseDto> InviteUserAsync(InviteUserDto request)
        {
            string email = request.Email.Trim().ToLowerInvariant();
            string username = string.IsNullOrWhiteSpace(request.Username)
                ? email
                : request.Username.Trim();

            ApplicationUser existingUserByEmail = await _userManager.FindByEmailAsync(email);
            if (existingUserByEmail != null)
            {
                throw new ArgumentException("A user with this email already exists.");
            }

            ApplicationUser existingUserByName = await _userManager.FindByNameAsync(username);
            if (existingUserByName != null)
            {
                throw new ArgumentException("A user with this username already exists.");
            }

            ApplicationUser user = new ApplicationUser
            {
                UserName = username,
                Email = email,
                EmailConfirmed = false,
            };

            IdentityResult createResult = await _userManager.CreateAsync(user);

            if (!createResult.Succeeded)
            {
                throw new ArgumentException(AuthHelpers.BuildIdentityErrorMessage(createResult));
            }

            string token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            string encodedToken = AuthHelpers.EncodeToken(token);

            string confirmUrl = AuthHelpers.BuildConfirmUrl(
                user.Id,
                encodedToken,
                _httpContextAccessor
            );

            string safeUserName = System.Net.WebUtility.HtmlEncode(username);

            string subject = "Confirm your account and set password";
            string message =
                $@"
                    <p>Hello {safeUserName},</p>
                    <p>Please click the link below to confirm your email and set your password:</p>
                    <p><a href=""{confirmUrl}"">Confirm Email / Set Password</a></p>";

            await _emailSender.SendEmailAsync(email, subject, message);

            return new InviteUserResponseDto
            {
                Message = "User invited successfully. Confirmation email sent.",
                UserId = user.Id,
                Email = user.Email ?? string.Empty,
            };
        }

        public async Task<ConfirmEmailResponseDto> ConfirmEmailAndSetPasswordAsync(
            SetPasswordDto request
        )
        {
            ApplicationUser user = await _userManager.FindByIdAsync(request.UserId);

            if (user == null)
            {
                throw new KeyNotFoundException("User not found.");
            }

            if (user.EmailConfirmed)
            {
                throw new ArgumentException("Email is already confirmed.");
            }

            string decodedToken = AuthHelpers.DecodeToken(request.Token);

            IdentityResult confirmResult = await _userManager.ConfirmEmailAsync(user, decodedToken);

            if (!confirmResult.Succeeded)
            {
                throw new ArgumentException(AuthHelpers.BuildIdentityErrorMessage(confirmResult));
            }

            IdentityResult passwordResult = await _userManager.AddPasswordAsync(
                user,
                request.Password
            );

            if (!passwordResult.Succeeded)
            {
                throw new ArgumentException(AuthHelpers.BuildIdentityErrorMessage(passwordResult));
            }

            return new ConfirmEmailResponseDto
            {
                Message = "Email confirmed and password set successfully. You can now log in.",
            };
        }
    }
}
