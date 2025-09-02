using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace TaskHub.Server;

public static class ScriptsEndpoints
{
    public static IEndpointRouteBuilder MapScriptEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/scripts", (ScriptsRepository repo) => Results.Ok(repo.GetAll()))
            .Produces<IEnumerable<ScriptItem>>();

        app.MapGet("/scripts/{id}", (string id, ScriptsRepository repo) =>
        {
            return repo.TryGet(id, out var item) && item != null ? Results.Ok(item) : Results.NotFound();
        }).Produces<ScriptItem>();

        app.MapPost("/scripts", (JsonDocument body, ScriptsRepository repo, ScriptSignatureVerifier sigVerifier) =>
        {
            var (item, error) = ParseBody(body);
            if (error != null) return Results.BadRequest(error);
            if (string.IsNullOrWhiteSpace(item!.Name)) return Results.BadRequest("Name is required");
            item.IsVerified = sigVerifier.IsAuthenticodeValid(item.Content);
            var saved = repo.CreateOrUpdate(item);
            return Results.Ok(saved);
        }).Produces<ScriptItem>();

        app.MapPut("/scripts/{id}", (string id, JsonDocument body, ScriptsRepository repo, ScriptSignatureVerifier sigVerifier) =>
        {
            var (item, error) = ParseBody(body);
            if (error != null) return Results.BadRequest(error);
            if (string.IsNullOrWhiteSpace(item!.Name)) return Results.BadRequest("Name is required");
            item.Id = id;
            item.IsVerified = sigVerifier.IsAuthenticodeValid(item.Content);
            var saved = repo.CreateOrUpdate(item);
            return Results.Ok(saved);
        }).Produces<ScriptItem>();

        app.MapDelete("/scripts/{id}", (string id, ScriptsRepository repo) =>
        {
            return repo.Delete(id) ? Results.Ok() : Results.NotFound();
        });

        return app;
    }

    private static (ScriptItem? Item, string? Error) ParseBody(JsonDocument body)
    {
        try
        {
            var root = body.RootElement;
            JsonElement payloadEl;
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("payload", out var p))
            {
                payloadEl = p;
            }
            else
            {
                payloadEl = root;
            }

            var item = JsonSerializer.Deserialize<ScriptItem>(payloadEl.GetRawText());
            if (item == null) return (null, "Invalid payload");
            return (item, null);
        }
        catch (JsonException)
        {
            return (null, "Malformed JSON");
        }
    }
}
