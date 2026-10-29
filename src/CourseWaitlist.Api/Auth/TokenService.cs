using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CourseWaitlist.Api.Models;
using Microsoft.IdentityModel.Tokens;
namespace CourseWaitlist.Api.Auth;

public interface ITokenService { TokenResponse CreateToken(string username); }
public sealed class TokenService(IConfiguration configuration) : ITokenService
{
    public TokenResponse CreateToken(string username)
    {
        var key = configuration["Jwt:Key"] ?? "DEV_ONLY_CHANGE_THIS_KEY_TO_A_LONG_RANDOM_SECRET_123!";
        var issuer = configuration["Jwt:Issuer"] ?? "CourseWaitlist";
        var audience = configuration["Jwt:Audience"] ?? "CourseWaitlist.Api";
        var expires = DateTime.UtcNow.AddMinutes(60);
        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(issuer, audience, new[] { new Claim(ClaimTypes.Name, username), new Claim(ClaimTypes.Role, "CourseManager") }, expires: expires, signingCredentials: credentials);
        return new TokenResponse(new JwtSecurityTokenHandler().WriteToken(token), "Bearer", 3600);
    }
}
