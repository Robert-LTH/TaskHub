using System;
using System.Net.Http.Headers;
using System.Text;
using Hangfire.Dashboard;

namespace TaskHub.Server;

public class BasicAuthAuthorizationFilter : IDashboardAuthorizationFilter
{
    private readonly string _username;
    private readonly string _password;

    public BasicAuthAuthorizationFilter(string username, string password)
    {
        _username = username;
        _password = password;
    }

    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        var header = httpContext.Request.Headers["Authorization"].ToString();
        if (!string.IsNullOrEmpty(header))
        {
            if (AuthenticationHeaderValue.TryParse(header, out var auth) &&
                "Basic".Equals(auth.Scheme, StringComparison.OrdinalIgnoreCase))
            {
                var credentialBytes = Convert.FromBase64String(auth.Parameter ?? string.Empty);
                var credentials = Encoding.UTF8.GetString(credentialBytes).Split(':', 2);
                if (credentials.Length == 2 && credentials[0] == _username && credentials[1] == _password)
                {
                    return true;
                }
            }
        }

        httpContext.Response.StatusCode = 401;
        httpContext.Response.Headers["WWW-Authenticate"] = "Basic";
        return false;
    }
}
