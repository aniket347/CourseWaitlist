using CourseWaitlist.Api.Auth;
using CourseWaitlist.Api.Models;
using Microsoft.AspNetCore.Mvc;
namespace CourseWaitlist.Api.Controllers;

[ApiController, Route("api/auth")]
public sealed class AuthController(ITokenService tokens) : ControllerBase
{
    // Demo-only credentials. Replace with real identity validation before production use.
    [HttpPost("token")]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status200OK)]
    public IActionResult GenerateToken([FromBody] TokenRequest request)
    {
        if (request.Username != "course-admin" || request.Password != "ChangeMe123!")
            return Unauthorized(new { message = "Invalid username or password." });
        return Ok(tokens.CreateToken(request.Username));
    }
}
