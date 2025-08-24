using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace TaskHub.Server;

public static class PluginEndpoints
{
    public static IEndpointRouteBuilder MapPluginEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/dlls", (PluginManager plugins) => plugins.LoadedAssemblies);
        return app;
    }
}
