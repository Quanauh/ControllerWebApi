using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

public class TokenService
{
    private readonly IConfiguration config;

    public TokenService(IConfiguration config)
    {
        this.config = config;
    }

    public string GenerateToken(string stk)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, stk)   // day chinh la "sub" - danh tinh nguoi dang nhap
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(double.Parse(config["Jwt:ExpiryMinutes"]!)),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}