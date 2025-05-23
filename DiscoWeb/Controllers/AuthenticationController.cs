using DiscoWeb.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace DiscoWeb.Controllers;

[Authorize]
[ApiController]
public class AuthenticationController(IConfiguration appSettings) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("authenticate")]
    public ActionResult<AuthResponse> Authenticate(AuthRequest authRequest)
    {
        var password = appSettings["JWT:PasswordHash"]!;
        if (authRequest.Hash != password)
        {
            return Unauthorized(new SimpleResponse
            {
                Status = "401",
                Message = "Invalid password"
            });
        }

        var token = CreateToken();
        return Ok(new AuthResponse { Token = token });
    }

    [Authorize]
    [HttpGet("validate")]
    public ActionResult<SimpleResponse> Validate()
    {
        return Ok(new SimpleResponse
        {
            Status = "200",
            Message = "Token is valid",
        });
    }

    private string CreateToken()
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(appSettings["JWT:Secret"]!);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
            Expires = DateTime.UtcNow.AddYears(100)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
