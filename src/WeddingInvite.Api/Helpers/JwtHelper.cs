using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace WeddingInvite.Api.Helpers;

public static class JwtHelper
{
    public static Guid GetTenantId(HttpContext httpContext)
    {
        var authHeader = httpContext.Request.Headers.Authorization.ToString();
        if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var token = authHeader["Bearer ".Length..].Trim();
            var handler = new JwtSecurityTokenHandler();
            if (handler.CanReadToken(token))
            {
                var jwt = handler.ReadJwtToken(token);
                var tenantClaim = jwt.Claims.FirstOrDefault(c => c.Type == "tenantId") 
                    ?? jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier) 
                    ?? jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);
                if (tenantClaim != null && Guid.TryParse(tenantClaim.Value, out var tenantIdFromToken))
                {
                    return tenantIdFromToken;
                }
            }
        }
        var claim = httpContext.User?.FindFirst("tenantId") 
            ?? httpContext.User?.FindFirst(ClaimTypes.NameIdentifier) 
            ?? httpContext.User?.FindFirst("sub") 
            ?? httpContext.User?.FindFirst(JwtRegisteredClaimNames.Sub);
        if (claim == null) throw new UnauthorizedAccessException("Missing tenant claim");
        return Guid.Parse(claim.Value);
    }
}
