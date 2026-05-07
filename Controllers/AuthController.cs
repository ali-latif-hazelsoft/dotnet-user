using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using dotnet_user.Data;
using dotnet_user.Dtos.Member;
using dotnet_user.Models;
using Microsoft.AspNetCore.Mvc;

namespace dotnet_user.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthRepository _authRepo;

        public AuthController(IAuthRepository authRepo)
        {
            _authRepo = authRepo;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(MemberRegisterDto request)
        {
            ServiceResponse<int> response = await _authRepo.Register(
                new Member { Username = request.Username },
                request.Password
            );
            if (!response.Success)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(MemberLoginDto request)
        {
            ServiceResponse<string> response = await _authRepo.Login(
                request.Username,
                request.Password
            );
            if (!response.Success)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }
    }
}
