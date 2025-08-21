# TaskHub

Sample Hangfire-based server with plugin-based command handlers and service plugins.

## Building

Requires the .NET 8 SDK.

```
dotnet build
```

## Running

Build the solution and run the server:

```
dotnet run --project src/TaskHub.Server
```

The server exposes a minimal API:

- `POST /commands/{handler}?arg=value` – enqueue a command handled by a plugin and return the job id with metadata.
- `POST /commands/{id}/cancel` – cancel a queued job.
- `GET /dlls` – list loaded plugin assemblies.

An OpenAPI/Swagger UI powered by NSwag is available at `/swagger`.

Plugins must be compiled and copied under the `plugins` directory as described in the project.
The `TaskHub.Abstractions` project contains shared interfaces and result types and can be packaged as a NuGet
dependency for plugin development.
