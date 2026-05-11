using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace dotnet_user.Models
{
    public class ApplicationUser : IdentityUser<int>
    {
        public ICollection<User> Profiles { get; set; }
    }
}
