namespace DocumentQA.Application.Configuration;

public class IngestionSettings
{
    public const string SectionName = "Ingestion";

    public int MaxFileSizeMb { get; set; } = 10;
    public int ChunkSize { get; set; } = 500;
    public int ChunkOverlap { get; set; } = 50;
    public string[] AllowedContentTypes { get; set; } = ["application/pdf", "text/plain", "text/markdown"];
}
