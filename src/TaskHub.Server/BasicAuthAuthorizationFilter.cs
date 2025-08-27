using System;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using Hangfire.Dashboard;

namespace TaskHub.Server;

public class BasicAuthAuthorizationFilter : IDashboardAuthorizationFilter
{
    private readonly byte[] _usernameHash;
    private readonly byte[] _passwordHash;

    public BasicAuthAuthorizationFilter(string username, string password)
    {
        using var sha256 = SHA256.Create();
        _usernameHash = sha256.ComputeHash(Encoding.UTF8.GetBytes(username ?? string.Empty));
        _passwordHash = sha256.ComputeHash(Encoding.UTF8.GetBytes(password ?? string.Empty));
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
                var parts = Encoding.UTF8.GetString(credentialBytes).Split(':', 2);
                if (parts.Length == 2)
                {
                    using var sha256 = SHA256.Create();
                    var userHash = sha256.ComputeHash(Encoding.UTF8.GetBytes(parts[0]));
                    var passHash = sha256.ComputeHash(Encoding.UTF8.GetBytes(parts[1]));
                    if (CryptographicOperations.FixedTimeEquals(userHash, _usernameHash) &&
                        CryptographicOperations.FixedTimeEquals(passHash, _passwordHash))
                    {
                        return true;
                    }
                }
            }
        }

        httpContext.Response.StatusCode = 401;
        httpContext.Response.Headers["WWW-Authenticate"] = "Basic";
        return false;
    }
}
