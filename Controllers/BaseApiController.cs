using System;
using Microsoft.AspNetCore.Mvc;

namespace dotnet_user.Controllers
{
    public abstract class BaseApiController : ControllerBase
    {
        protected IActionResult OkResponse(string message = "Success")
        {
            return Ok(new { success = true, message });
        }

        protected IActionResult OkResponse<T>(T data, string message = "Success")
        {
            return Ok(
                new
                {
                    success = true,
                    message,
                    data,
                }
            );
        }

        protected IActionResult BadRequestResponse(string message)
        {
            return BadRequest(new { success = false, message });
        }

        protected IActionResult BadRequestResponse(Exception ex)
        {
            return BadRequestResponse(ex.Message);
        }
    }
}
