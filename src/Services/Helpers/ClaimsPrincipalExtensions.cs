using System.Security.Claims;

namespace MediaBridge.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static int GetUserId(this ClaimsPrincipal principal)
        {
            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
            
            if (userIdClaim == null)
                throw new UnauthorizedAccessException("User ID not found in token");
                
            if (!int.TryParse(userIdClaim.Value, out int userId))
                throw new UnauthorizedAccessException("Invalid user ID in token");
                
            return userId;
        }

        public static string GetUsername(this ClaimsPrincipal principal)
        {
            var usernameClaim = principal.FindFirst(ClaimTypes.Name);
            
            if (usernameClaim == null)
                throw new UnauthorizedAccessException("Username not found in token");
                
            return usernameClaim.Value;
        }

        public static string? GetUserEmail(this ClaimsPrincipal principal)
        {
            return principal.FindFirst(ClaimTypes.Email)?.Value;
        }

        public static List<string> GetUserRoles(this ClaimsPrincipal principal)
        {
            return principal.FindAll(ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();
        }

        public static bool IsInRole(this ClaimsPrincipal principal, params string[] roles)
        {
            var userRoles = principal.GetUserRoles();
            return roles.Any(role => userRoles.Contains(role));
        }
    }
}