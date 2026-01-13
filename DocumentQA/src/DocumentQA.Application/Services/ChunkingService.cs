using System.Text;
using System.Text.RegularExpressions;
using DocumentQA.Application.Configuration;
using Microsoft.Extensions.Options;

namespace DocumentQA.Application.Services;

public interface IChunkingService
{
    List<ChunkResult> Chunk(string text);
}

public record ChunkResult(string Content, int Index, int TokenEstimate);

public partial class ChunkingService : IChunkingService
{
    private readonly IngestionSettings _settings;

    public ChunkingService(IOptions<IngestionSettings> settings)
    {
        _settings = settings.Value;
    }

    public List<ChunkResult> Chunk(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        var sentences = SplitIntoSentences(text);
        var chunks = new List<ChunkResult>();
        var currentChunk = new StringBuilder();
        var overlapBuffer = new Queue<string>();
        var chunkIndex = 0;

        foreach (var sentence in sentences)
        {
            var sentenceTokens = EstimateTokens(sentence);
            var currentTokens = EstimateTokens(currentChunk.ToString());

            if (currentTokens + sentenceTokens > _settings.ChunkSize && currentChunk.Length > 0)
            {
                // Save current chunk
                var content = currentChunk.ToString().Trim();
                chunks.Add(new ChunkResult(content, chunkIndex++, EstimateTokens(content)));

                // Build overlap from buffer
                currentChunk.Clear();
                foreach (var overlapSentence in overlapBuffer)
                {
                    currentChunk.Append(overlapSentence).Append(' ');
                }
            }

            currentChunk.Append(sentence).Append(' ');

            // Maintain overlap buffer
            overlapBuffer.Enqueue(sentence);
            while (EstimateTokens(string.Join(" ", overlapBuffer)) > _settings.ChunkOverlap && overlapBuffer.Count > 1)
            {
                overlapBuffer.Dequeue();
            }
        }

        // Don't forget last chunk
        if (currentChunk.Length > 0)
        {
            var content = currentChunk.ToString().Trim();
            chunks.Add(new ChunkResult(content, chunkIndex, EstimateTokens(content)));
        }

        return chunks;
    }

    private static string[] SplitIntoSentences(string text)
    {
        // Split on sentence boundaries
        return SentenceRegex().Split(text)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToArray();
    }

    private static int EstimateTokens(string text)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        // Rough estimation: ~4 characters per token for English
        return (int)Math.Ceiling(text.Length / 4.0);
    }

    [GeneratedRegex(@"(?<=[.!?])\s+", RegexOptions.Compiled)]
    private static partial Regex SentenceRegex();
}
