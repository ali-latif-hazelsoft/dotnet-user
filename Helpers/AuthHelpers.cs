using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using dotnet_user.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace dotnet_user.Helpers
{
    public static class AuthHelpers
    {
        public static string GenerateJwtToken(ApplicationUser user, IConfiguration configuration)
        {
            string tokenKey = configuration["AppSettings:Token"];

            if (string.IsNullOrWhiteSpace(tokenKey))
            {
                throw new InvalidOperationException("JWT token key is missing.");
            }

            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
            };

            SymmetricSecurityKey key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenKey));
            SigningCredentials creds = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha512Signature
            );

            JwtSecurityToken token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddDays(1),
                signingCredentials: creds
            );

            JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
            return tokenHandler.WriteToken(token);
        }

        public static string BuildConfirmUrl(
            int userId,
            string encodedToken,
            IHttpContextAccessor httpContextAccessor
        )
        {
            HttpContext httpContext = httpContextAccessor.HttpContext;

            if (httpContext == null)
            {
                throw new InvalidOperationException("HTTP context is not available.");
            }

            string scheme = httpContext.Request.Scheme;
            string host = httpContext.Request.Host.Value ?? string.Empty;

            return $"{scheme}://{host}/auth/confirmEmailAndSetPassword?userId={userId}&token={encodedToken}";
        }

        public static string BuildIdentityErrorMessage(IdentityResult result)
        {
            string message = string.Join(
                ", ",
                result.Errors.Select((IdentityError error) => error.Description)
            );

            if (string.IsNullOrWhiteSpace(message))
            {
                message = "Identity operation failed.";
            }

            return message;
        }

        public static string EncodeToken(string token)
        {
            return WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        }

        public static string DecodeToken(string encodedToken)
        {
            return Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(encodedToken));
        }
    }
}
