using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Babylon.Alfred.Api.Shared.Data.Models;
using Microsoft.IdentityModel.Tokens;

namespace Babylon.Alfred.Api.Features.Authentication.Utils;

public class JwtTokenGenerator(IConfiguration configuration)
{
    public string GenerateToken(User user)
    {
        var secretKey = configuration["Authentication:Jwt:SecretKey"];
        var issuer = configuration["Authentication:Jwt:Issuer"];
        var audience = configuration["Authentication:Jwt:Audience"];
        var expirationMinutes = int.Parse(configuration["Authentication:Jwt:ExpirationMinutes"] ?? "1440");

        if (string.IsNullOrEmpty(secretKey))
        {
            throw new InvalidOperationException("JWT SecretKey is not configured.");
        }

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.UniqueName, user.Username),
            new("AuthProvider", user.AuthProvider ?? "Local")
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
