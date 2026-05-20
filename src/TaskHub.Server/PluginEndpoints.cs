using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;

namespace TaskHub.Server;

public static class PluginEndpoints
{
    public static IEndpointRouteBuilder MapPluginEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/dlls", (PluginManager plugins) => plugins.LoadedAssemblies)
            .RequireAuthorization("CommandExecutor");
        return app;
    }
}
