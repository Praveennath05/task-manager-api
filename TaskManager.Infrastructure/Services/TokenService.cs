using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace TaskManager.Infrastructure.Services;

public class TokenService
{
    // ── CONFIGURATION ─────────────────────────────────────
    // IConfiguration reads values from appsettings.json
    // We use it to get SecretKey, Issuer, Audience, ExpiryMinutes
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(IdentityUser user, IList<string> roles)
    {
        // ── CLAIMS ────────────────────────────────────────
        // Claims are pieces of information stored inside the token
        // Like a passport — it contains your name, id, email, role
        // The client sends this token with every request
        // Your API reads these claims to know WHO is making the request
        var claims = new List<Claim>
        {
            new Claim (ClaimTypes.NameIdentifier , user.Id),
            new Claim(ClaimTypes.Email, user .Email!),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        // Add roles as claims — Admin or User
        // This is how role-based access works
        foreach(var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }
        // ── SIGNING KEY ───────────────────────────────────
        // The SecretKey from appsettings.json is converted to bytes
        // Used to sign the token — proves it came from YOUR server
        // If someone tampers with the token, the signature breaks
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["JwtSettings:SecretKey"]!));


        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
         // ── TOKEN CREATION ────────────────────────────────
        // Build the actual JWT token with all settings
        var token = new JwtSecurityToken(
            issuer: _configuration["JwtSettings:Issuer"],
            audience:_configuration["JwtSettings:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(
                double.Parse(_configuration["JwtSettings:ExpiryMinutes"]!)),
                signingCredentials: credentials
            );
            // ── SERIALIZE ─────────────────────────────────────
        // Convert the token object to a string
        // This is the actual JWT string sent to the client
        // Looks like: eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIx...
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

}

