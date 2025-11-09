using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MediaBridge.Database.DB_Models;
using MediaBridge.Models.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace MediaBridge.Services.Authentication
{
    public class TokenService : ITokenService
    {
        private readonly JwtOptions _jwtOptions;

        public TokenService(IOptions<JwtOptions> jwtOptions)
        {
            _jwtOptions = jwtOptions.Value;
        }

        public string GenerateToken(User user)
        {
            var keyBytes = Encoding.UTF8.GetBytes(_jwtOptions.Key);
            var securityKey = new SymmetricSecurityKey(keyBytes);
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
            };

            foreach (var userRole in user.UserRoles)
            {
                if (userRole.Role != null && !string.IsNullOrWhiteSpace(userRole.Role.RoleValue))
                {
                    claims.Add(new Claim(ClaimTypes.Role, userRole.Role.RoleValue)); // "Admin", "Maintainer", "User"
                }
            }

            var token = new JwtSecurityToken(
                issuer: _jwtOptions.Issuer,
                audience: _jwtOptions.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtOptions.ExpiresMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
