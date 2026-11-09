using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using HuanBank.Api.Authorization;
namespace HuanBank.Api.Util;
public class TokenService
{
    private readonly IConfiguration _config;

    public TokenService(IConfiguration config)
    {
        this._config = config;
    }

    public string GenerateToken(string stk)
    {
        var adminAccounts = _config.GetSection("Authorization:AdminAccounts").Get<string[]>() ?? [];
        var role = adminAccounts.Contains(stk, StringComparer.Ordinal)
            ? BankRoles.Admin
            : BankRoles.Customer;

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, stk),
            new Claim(ClaimTypes.Role, role)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(double.Parse(_config["Jwt:ExpiryMinutes"]!)),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
