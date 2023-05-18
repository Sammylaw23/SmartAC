using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Theoremone.SmartAc.Application.Constants;
using Theoremone.SmartAc.Application.Interfaces.Identity;

namespace Theoremone.SmartAc.Application.Services.Identity;

public class SmartAcJwtService : ISmartAcJwtService
{
    private readonly string _key;
    private readonly string _issuer;
    private readonly string _audience;
    public SmartAcJwtService(IConfiguration configuration)
    {
        _key = configuration["Jwt:Key"];
        _issuer = configuration["Jwt:Issuer"];
        _audience = configuration["Jwt:Audience"];
    }
    public (string tokenId, string token) GenerateJwtFor(string targetId, string role)
    {
        var tokenId = Guid.NewGuid().ToString();

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, targetId),
            new Claim(JwtRegisteredClaimNames.Jti, tokenId),
            new Claim(JwtRegisteredClaimNames.Iat, DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)),
            new Claim(ClaimTypes.Role, SmartJwtServiceConstants.JwtScopeApiKey),
            new Claim(ClaimTypes.Role, role)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: credentials);

        return (tokenId, new JwtSecurityTokenHandler().WriteToken(token));
    }
}