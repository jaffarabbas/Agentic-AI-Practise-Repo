using System.Text;
using DocumentQA.Application.Interfaces;
using UglyToad.PdfPig;

namespace DocumentQA.Infrastructure.FileProcessing;

public class TextExtractor : ITextExtractor
{
    private readonly HashSet<string> _supportedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "text/plain",
        "text/markdown"
    };

    public bool CanExtract(string contentType)
    {
        return _supportedTypes.Contains(contentType);
    }

    public async Task<string> ExtractAsync(Stream fileStream, string contentType, CancellationToken ct = default)
    {
        return contentType.ToLowerInvariant() switch
        {
            "application/pdf" => ExtractFromPdf(fileStream),
            "text/plain" or "text/markdown" => await ExtractFromTextAsync(fileStream, ct),
            _ => throw new NotSupportedException($"Content type '{contentType}' is not supported.")
        };
    }

    private static string ExtractFromPdf(Stream stream)
    {
        var sb = new StringBuilder();

        using var document = PdfDocument.Open(stream);
        foreach (var page in document.GetPages())
        {
            sb.AppendLine(page.Text);
        }

        return sb.ToString();
    }

    private static async Task<string> ExtractFromTextAsync(Stream stream, CancellationToken ct)
    {
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync(ct);
    }
}
