using System.Runtime.CompilerServices;
using DocumentQA.Application.DTOs;
using DocumentQA.Application.Services;

namespace DocumentQA.Api.Endpoints;

public static class ChatEndpoints
{
    public static void MapChatEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api")
            .WithTags("Chat")
            .WithOpenApi();

        group.MapPost("/ask", Ask)
            .Produces<AskResponse>()
            .Produces(StatusCodes.Status400BadRequest);

        group.MapPost("/ask/stream", AskStream)
            .Produces<string>(StatusCodes.Status200OK, "text/event-stream");
    }

    private static async Task<IResult> Ask(
        AskRequest request,
        IRagService ragService,
        HttpContext context,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
            return Results.BadRequest("Question is required.");

        var userId = context.Request.Headers["X-User-Id"].FirstOrDefault() ?? "anonymous";

        try
        {
            var response = await ragService.AskAsync(userId, request, ct);
            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            return Results.Problem(ex.Message);
        }
    }

    private static async IAsyncEnumerable<string> AskStream(
        AskRequest request,
        IRagService ragService,
        HttpContext context,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var userId = context.Request.Headers["X-User-Id"].FirstOrDefault() ?? "anonymous";

        await foreach (var chunk in ragService.AskStreamAsync(userId, request, ct))
        {
            yield return chunk;
        }
    }
}
