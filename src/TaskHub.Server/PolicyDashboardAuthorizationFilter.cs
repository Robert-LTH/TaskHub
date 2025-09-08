using Hangfire.Dashboard;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace TaskHub.Server;

public class PolicyDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    private readonly string _policyName;

    public PolicyDashboardAuthorizationFilter(string policyName)
    {
        _policyName = policyName;
    }

    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        var authz = httpContext.RequestServices.GetService<IAuthorizationService>();
        if (authz == null) return false;

        // Ensure we trigger an auth challenge (Negotiate/Bearer via policy scheme)
        if (httpContext.User?.Identity?.IsAuthenticated != true)
        {
            // Uses the app's default challenge scheme (our policy scheme forwards to Negotiate when no Bearer)
            httpContext.ChallengeAsync().GetAwaiter().GetResult();
            return false;
        }
        var result = authz.AuthorizeAsync(httpContext.User, null, _policyName).GetAwaiter().GetResult();
        if (!result.Succeeded)
        {
            httpContext.Response.StatusCode = 403;
        }
        return result.Succeeded;
    }
}
