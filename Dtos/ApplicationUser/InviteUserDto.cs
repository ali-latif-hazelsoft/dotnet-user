using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace dotnet_user.Dtos.ApplicationUser
{
    public class InviteUserDto
    {
        [Required]
        public string Username { get; set; }

        [Required]
        public string Email { get; set; }
    }
}
