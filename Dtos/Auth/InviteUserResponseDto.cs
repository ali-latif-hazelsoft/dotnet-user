using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace dotnet_user.Dtos.Auth
{
    public class InviteUserResponseDto
    {
        public string Message { get; set; } = string.Empty;
        public int UserId { get; set; } = 0;
        public string Email { get; set; } = string.Empty;
    }
}
