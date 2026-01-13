using Dapper;
using DocumentQA.Application.Interfaces;
using DocumentQA.Domain.Entities;
using Npgsql;

namespace DocumentQA.Infrastructure.Persistence;

public class DocumentRepository : IDocumentRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public DocumentRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<Guid> CreateAsync(Document document, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO documents (id, user_id, filename, content_type, file_size_bytes, status, created_at)
            VALUES (@Id, @UserId, @Filename, @ContentType, @FileSizeBytes, @Status, @CreatedAt)
            RETURNING id
            """;

        await using var connection = await _dataSource.OpenConnectionAsync(ct);
        return await connection.ExecuteScalarAsync<Guid>(sql, new
        {
            document.Id,
            document.UserId,
            document.Filename,
            document.ContentType,
            document.FileSizeBytes,
            Status = document.Status.ToString().ToLower(),
            document.CreatedAt
        });
    }

    public async Task<Document?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = """
            SELECT id, user_id as UserId, filename, content_type as ContentType,
                   file_size_bytes as FileSizeBytes, chunk_count as ChunkCount,
                   status, error_message as ErrorMessage, created_at as CreatedAt,
                   processed_at as ProcessedAt
            FROM documents
            WHERE id = @Id
            """;

        await using var connection = await _dataSource.OpenConnectionAsync(ct);
        var result = await connection.QuerySingleOrDefaultAsync<DocumentRow>(sql, new { Id = id });

        return result == null ? null : MapToDocument(result);
    }

    public async Task<List<Document>> GetByUserIdAsync(string userId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT id, user_id as UserId, filename, content_type as ContentType,
                   file_size_bytes as FileSizeBytes, chunk_count as ChunkCount,
                   status, error_message as ErrorMessage, created_at as CreatedAt,
                   processed_at as ProcessedAt
            FROM documents
            WHERE user_id = @UserId
            ORDER BY created_at DESC
            """;

        await using var connection = await _dataSource.OpenConnectionAsync(ct);
        var results = await connection.QueryAsync<DocumentRow>(sql, new { UserId = userId });

        return results.Select(MapToDocument).ToList();
    }

    public async Task UpdateStatusAsync(
        Guid id,
        DocumentStatus status,
        int? chunkCount = null,
        string? errorMessage = null,
        CancellationToken ct = default)
    {
        const string sql = """
            UPDATE documents
            SET status = @Status,
                chunk_count = COALESCE(@ChunkCount, chunk_count),
                error_message = @ErrorMessage,
                processed_at = CASE WHEN @Status IN ('completed', 'failed') THEN NOW() ELSE processed_at END
            WHERE id = @Id
            """;

        await using var connection = await _dataSource.OpenConnectionAsync(ct);
        await connection.ExecuteAsync(sql, new
        {
            Id = id,
            Status = status.ToString().ToLower(),
            ChunkCount = chunkCount,
            ErrorMessage = errorMessage
        });
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = "DELETE FROM documents WHERE id = @Id";

        await using var connection = await _dataSource.OpenConnectionAsync(ct);
        await connection.ExecuteAsync(sql, new { Id = id });
    }

    private static Document MapToDocument(DocumentRow row)
    {
        return new Document
        {
            Id = row.Id,
            UserId = row.UserId,
            Filename = row.Filename,
            ContentType = row.ContentType,
            FileSizeBytes = row.FileSizeBytes,
            ChunkCount = row.ChunkCount,
            Status = Enum.Parse<DocumentStatus>(row.Status, ignoreCase: true),
            ErrorMessage = row.ErrorMessage,
            CreatedAt = row.CreatedAt,
            ProcessedAt = row.ProcessedAt
        };
    }

    private record DocumentRow(
        Guid Id,
        string UserId,
        string Filename,
        string? ContentType,
        long? FileSizeBytes,
        int ChunkCount,
        string Status,
        string? ErrorMessage,
        DateTime CreatedAt,
        DateTime? ProcessedAt
    );
}
