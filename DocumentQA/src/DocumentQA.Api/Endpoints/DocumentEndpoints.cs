using DocumentQA.Application.DTOs;
using DocumentQA.Application.Interfaces;
using DocumentQA.Application.Services;

namespace DocumentQA.Api.Endpoints;

public static class DocumentEndpoints
{
    public static void MapDocumentEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/documents")
            .WithTags("Documents")
            .WithOpenApi();

        group.MapPost("/", UploadDocument)
            .DisableAntiforgery()
            .Accepts<IFormFile>("multipart/form-data")
            .Produces<UploadDocumentResponse>(StatusCodes.Status202Accepted)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapGet("/", GetUserDocuments)
            .Produces<List<DocumentDto>>();

        group.MapGet("/{id:guid}", GetDocument)
            .Produces<DocumentDto>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", DeleteDocument)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> UploadDocument(
        IFormFile file,
        IIngestionService ingestionService,
        HttpContext context,
        CancellationToken ct)
    {
        // TODO: Replace with actual user ID from auth
        var userId = context.Request.Headers["X-User-Id"].FirstOrDefault() ?? "anonymous";

        if (file.Length == 0)
            return Results.BadRequest("File is empty.");

        try
        {
            await using var stream = file.OpenReadStream();
            var documentId = await ingestionService.QueueDocumentAsync(
                userId,
                file.FileName,
                file.ContentType,
                stream,
                ct);

            return Results.Accepted(
                $"/api/documents/{documentId}",
                new UploadDocumentResponse(documentId, "pending", "Document queued for processing."));
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(ex.Message);
        }
    }

    private static async Task<IResult> GetUserDocuments(
        IDocumentRepository documentRepository,
        HttpContext context,
        CancellationToken ct)
    {
        var userId = context.Request.Headers["X-User-Id"].FirstOrDefault() ?? "anonymous";
        var documents = await documentRepository.GetByUserIdAsync(userId, ct);

        var dtos = documents.Select(d => new DocumentDto(
            d.Id,
            d.Filename,
            d.ContentType,
            d.FileSizeBytes,
            d.ChunkCount,
            d.Status.ToString(),
            d.ErrorMessage,
            d.CreatedAt,
            d.ProcessedAt
        )).ToList();

        return Results.Ok(dtos);
    }

    private static async Task<IResult> GetDocument(
        Guid id,
        IDocumentRepository documentRepository,
        CancellationToken ct)
    {
        var document = await documentRepository.GetByIdAsync(id, ct);

        if (document == null)
            return Results.NotFound();

        return Results.Ok(new DocumentDto(
            document.Id,
            document.Filename,
            document.ContentType,
            document.FileSizeBytes,
            document.ChunkCount,
            document.Status.ToString(),
            document.ErrorMessage,
            document.CreatedAt,
            document.ProcessedAt
        ));
    }

    private static async Task<IResult> DeleteDocument(
        Guid id,
        IDocumentRepository documentRepository,
        IVectorRepository vectorRepository,
        CancellationToken ct)
    {
        var document = await documentRepository.GetByIdAsync(id, ct);

        if (document == null)
            return Results.NotFound();

        await vectorRepository.DeleteByDocumentIdAsync(id, ct);
        await documentRepository.DeleteAsync(id, ct);

        return Results.NoContent();
    }
}
