using Dapper;
using DocumentQA.Application.Interfaces;
using DocumentQA.Domain.Entities;
using Npgsql;
using Pgvector;

namespace DocumentQA.Infrastructure.Persistence;

public class VectorRepository : IVectorRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public VectorRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task InsertChunkAsync(Chunk chunk, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO chunks (id, document_id, content, embedding, chunk_index, token_count, created_at)
            VALUES ($1, $2, $3, $4, $5, $6, $7)
            """;

        await using var connection = await _dataSource.OpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, connection);

        cmd.Parameters.AddWithValue(chunk.Id);
        cmd.Parameters.AddWithValue(chunk.DocumentId);
        cmd.Parameters.AddWithValue(chunk.Content);
        cmd.Parameters.AddWithValue(new Vector(chunk.Embedding));
        cmd.Parameters.AddWithValue(chunk.ChunkIndex);
        cmd.Parameters.AddWithValue(chunk.TokenCount ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue(chunk.CreatedAt);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task InsertChunksBatchAsync(IEnumerable<Chunk> chunks, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO chunks (id, document_id, content, embedding, chunk_index, token_count, created_at)
            VALUES ($1, $2, $3, $4, $5, $6, $7)
            """;

        await using var connection = await _dataSource.OpenConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);

        try
        {
            foreach (var chunk in chunks)
            {
                await using var cmd = new NpgsqlCommand(sql, connection, transaction);

                cmd.Parameters.AddWithValue(chunk.Id);
                cmd.Parameters.AddWithValue(chunk.DocumentId);
                cmd.Parameters.AddWithValue(chunk.Content);
                cmd.Parameters.AddWithValue(new Vector(chunk.Embedding));
                cmd.Parameters.AddWithValue(chunk.ChunkIndex);
                cmd.Parameters.AddWithValue(chunk.TokenCount ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue(chunk.CreatedAt);

                await cmd.ExecuteNonQueryAsync(ct);
            }

            await transaction.CommitAsync(ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<List<SearchResult>> SearchAsync(
        float[] queryEmbedding,
        int topK,
        float minSimilarity,
        string? userId = null,
        Guid? documentId = null,
        CancellationToken ct = default)
    {
        var sqlBase = """
            SELECT c.id, c.document_id, c.content,
                   1 - (c.embedding <=> $1) as similarity,
                   d.filename
            FROM chunks c
            JOIN documents d ON c.document_id = d.id
            WHERE 1 - (c.embedding <=> $1) >= $2
            """;

        var paramIndex = 3;
        var conditions = new List<string>();

        if (userId != null)
            conditions.Add($"d.user_id = ${paramIndex++}");

        if (documentId != null)
            conditions.Add($"c.document_id = ${paramIndex++}");

        if (conditions.Count > 0)
            sqlBase += " AND " + string.Join(" AND ", conditions);

        sqlBase += $"""

            ORDER BY c.embedding <=> $1
            LIMIT ${paramIndex}
            """;

        await using var connection = await _dataSource.OpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(sqlBase, connection);

        cmd.Parameters.AddWithValue(new Vector(queryEmbedding));
        cmd.Parameters.AddWithValue(minSimilarity);

        if (userId != null)
            cmd.Parameters.AddWithValue(userId);

        if (documentId != null)
            cmd.Parameters.AddWithValue(documentId);

        cmd.Parameters.AddWithValue(topK);

        var results = new List<SearchResult>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        while (await reader.ReadAsync(ct))
        {
            results.Add(new SearchResult
            {
                ChunkId = reader.GetGuid(0),
                DocumentId = reader.GetGuid(1),
                Content = reader.GetString(2),
                Similarity = reader.GetFloat(3),
                Filename = reader.IsDBNull(4) ? null : reader.GetString(4)
            });
        }

        return results;
    }

    public async Task DeleteByDocumentIdAsync(Guid documentId, CancellationToken ct = default)
    {
        const string sql = "DELETE FROM chunks WHERE document_id = $1";

        await using var connection = await _dataSource.OpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue(documentId);
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
