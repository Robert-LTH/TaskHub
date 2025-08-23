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

- `POST /commands` – enqueue one or more commands handled by plugins. The body accepts a JSON payload with a
  `commands` array and an optional `payload` object. Returns the job id with metadata.
- `GET /commands/{id}` – retrieve the status of a previously enqueued job.
- `POST /commands/{id}/cancel` – cancel a queued job.
- `GET /dlls` – list loaded plugin assemblies.

An OpenAPI/Swagger UI powered by NSwag is available at `/swagger`.

Plugins must be compiled and copied under the `plugins` directory as described in the project.
The `TaskHub.Abstractions` project contains shared interfaces and result types and can be packaged as a NuGet
dependency for plugin development.

## ESLint

For JavaScript or TypeScript plugins, run ESLint to ensure code quality:

```bash
npm test
```

If no `test` script is defined, you can run ESLint directly:

```bash
npx eslint .
```

