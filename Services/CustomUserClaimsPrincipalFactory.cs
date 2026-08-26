using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using BibekSchool.Models;

namespace BibekSchool.Services
{
    public class CustomUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>
    {
        public CustomUserClaimsPrincipalFactory(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IOptions<IdentityOptions> optionsAccessor)
            : base(userManager, roleManager, optionsAccessor)
        {
        }

        protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
        {
            var identity = await base.GenerateClaimsAsync(user);

            // Ensure roles are added as claims
            var roles = await UserManager.GetRolesAsync(user);
            foreach (var role in roles)
            {
                if (!identity.HasClaim(c => c.Type == ClaimTypes.Role && c.Value == role))
                {
                    identity.AddClaim(new Claim(ClaimTypes.Role, role));
                }
            }

            // Add custom claims
            if (!identity.HasClaim(c => c.Type == "FullName"))
            {
                identity.AddClaim(new Claim("FullName", user.FullName ?? string.Empty));
            }

            if (!identity.HasClaim(c => c.Type == "IsActive"))
            {
                identity.AddClaim(new Claim("IsActive", user.IsActive.ToString()));
            }

            return identity;
        }
    }
}