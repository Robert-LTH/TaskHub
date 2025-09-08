using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;

namespace TaskHub.Server;

// Adds role claims based on SID claims so SIDs are treated as roles
public class SidToRoleClaimsTransformer : IClaimsTransformation
{
    private readonly IDictionary<string, string[]> _sidToRoles;
    private static readonly string[] SidClaimTypes = new[]
    {
        ClaimTypes.Sid,
        "http://schemas.microsoft.com/ws/2008/06/identity/claims/groupsid",
        "sid",
        "groupsid"
    };

    public SidToRoleClaimsTransformer(IConfiguration configuration)
    {
        // Authorization:SidMappings:<SID>:0..N = Role names
        var section = configuration.GetSection("Authorization:SidMappings");
        _sidToRoles = section.GetChildren()
            .ToDictionary(
                s => s.Key,
                s => s.Get<string[]>() ?? System.Array.Empty<string>(),
                System.StringComparer.OrdinalIgnoreCase);
    }

    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var identity = principal.Identity as ClaimsIdentity;
        if (identity == null)
        {
            return Task.FromResult(principal);
        }

        var presentSids = principal.Claims
            .Where(c => SidClaimTypes.Contains(c.Type))
            .Select(c => c.Value)
            .Distinct(System.StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (presentSids.Length == 0)
        {
            return Task.FromResult(principal);
        }

        // 1) Treat each SID as a role value so policies can list SIDs in Roles
        foreach (var sid in presentSids)
        {
            if (!principal.HasClaim(ClaimTypes.Role, sid))
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, sid));
            }
        }

        // 2) Back-compat: also add mapped role names from SidMappings
        foreach (var sid in presentSids)
        {
            if (_sidToRoles.TryGetValue(sid, out var roles))
            {
                foreach (var role in roles)
                {
                    if (!principal.HasClaim(ClaimTypes.Role, role))
                    {
                        identity.AddClaim(new Claim(ClaimTypes.Role, role));
                    }
                }
            }
        }

        return Task.FromResult(principal);
    }
}

