using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace InventoryManagementApi.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        // Obtains the id of the user from the JWT they have
        public static int GetUserId(this ClaimsPrincipal user)
        {
            // Uses JwtRegisteredClaimNames.Sub for the user ID
            var idClaim = user.FindFirst(JwtRegisteredClaimNames.Sub);

            if (idClaim == null)
            {
                throw new InvalidOperationException("User ID claim ('sub') not found in JWT.");
            }

            return int.Parse(idClaim.Value);
        }
    }
}
