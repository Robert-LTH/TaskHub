using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace TaskHub.Server;

// Logs user claims at Debug level when authorization fails to satisfy a policy
public class LoggingAuthorizationMiddlewareResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();
    private readonly ILogger<LoggingAuthorizationMiddlewareResultHandler> _logger;

    public LoggingAuthorizationMiddlewareResultHandler(ILogger<LoggingAuthorizationMiddlewareResultHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
    {
        if (!authorizeResult.Succeeded && _logger.IsEnabled(LogLevel.Debug))
        {
            try
            {
                var user = context.User;
                var name = user?.Identity?.Name ?? "anonymous";
                var authTypes = string.Join(
                    ", ",
                    user?.Identities?.Where(i => i.IsAuthenticated).Select(i => i.AuthenticationType ?? "") ?? Array.Empty<string>());

                var claimsDump = string.Join(
                    "; ",
                    user?.Claims?.Select(c => $"{c.Type}={c.Value}") ?? Array.Empty<string>());

                var requiredRoles = string.Join(
                    ", ",
                    policy?.Requirements?.OfType<RolesAuthorizationRequirement>()
                        .SelectMany(r => r.AllowedRoles ?? Array.Empty<string>())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                    ?? Array.Empty<string>());

                _logger.LogDebug(
                    "Authorization failed. Required roles: [{RequiredRoles}]. User: {User}. AuthTypes: [{AuthTypes}]. Claims: [{Claims}]",
                    requiredRoles,
                    name,
                    authTypes,
                    claimsDump);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Authorization failed and claim dump encountered an error.");
            }
        }

        await _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
    }
}

