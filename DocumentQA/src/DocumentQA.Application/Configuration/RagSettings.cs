namespace DocumentQA.Application.Configuration;

public class RagSettings
{
    public const string SectionName = "Rag";

    public int TopK { get; set; } = 5;
    public float MinSimilarity { get; set; } = 0.7f;
    public int MaxContextTokens { get; set; } = 4000;
}
