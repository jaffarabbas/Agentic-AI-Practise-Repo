using System.ClientModel;
using DocumentQA.Application.Configuration;
using DocumentQA.Application.Interfaces;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Embeddings;

namespace DocumentQA.Infrastructure.AI;

public class OpenAIEmbeddingService : IEmbeddingService
{
    private readonly EmbeddingClient _client;

    public OpenAIEmbeddingService(IOptions<OpenAISettings> settings)
    {
        var openAiClient = new OpenAIClient(settings.Value.ApiKey);
        _client = openAiClient.GetEmbeddingClient(settings.Value.EmbeddingModel);
    }

    public async Task<float[]> EmbedAsync(string text, CancellationToken ct = default)
    {
        var response = await _client.GenerateEmbeddingAsync(text, cancellationToken: ct);
        return response.Value.ToFloats().ToArray();
    }

    public async Task<float[][]> EmbedBatchAsync(IEnumerable<string> texts, CancellationToken ct = default)
    {
        var textList = texts.ToList();

        if (textList.Count == 0)
            return [];

        var response = await _client.GenerateEmbeddingsAsync(textList, cancellationToken: ct);

        return response.Value
            .OrderBy(e => e.Index)
            .Select(e => e.ToFloats().ToArray())
            .ToArray();
    }
}
