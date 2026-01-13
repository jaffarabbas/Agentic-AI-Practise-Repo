using DocumentQA.Api.Endpoints;
using DocumentQA.Application.BackgroundJobs;
using DocumentQA.Application.Configuration;
using DocumentQA.Application.Interfaces;
using DocumentQA.Application.Services;
using DocumentQA.Infrastructure.AI;
using DocumentQA.Infrastructure.FileProcessing;
using DocumentQA.Infrastructure.Persistence;
using Npgsql;
using Dapper;
using Pgvector;

// Register Dapper type handler for pgvector
SqlMapper.AddTypeHandler(new VectorTypeHandler());

var builder = WebApplication.CreateBuilder(args);

// Configuration
builder.Services.Configure<OpenAISettings>(builder.Configuration.GetSection(OpenAISettings.SectionName));
builder.Services.Configure<IngestionSettings>(builder.Configuration.GetSection(IngestionSettings.SectionName));
builder.Services.Configure<RagSettings>(builder.Configuration.GetSection(RagSettings.SectionName));

// Database
builder.Services.AddSingleton<NpgsqlDataSource>(sp =>
{
    var connectionString = builder.Configuration.GetConnectionString("Postgres")
        ?? throw new InvalidOperationException("Postgres connection string is required.");

    var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
    dataSourceBuilder.UseVector();
    return dataSourceBuilder.Build();
});

// Repositories
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<IVectorRepository, VectorRepository>();

// AI Services
builder.Services.AddSingleton<IEmbeddingService, OpenAIEmbeddingService>();
builder.Services.AddSingleton<IChatService, OpenAIChatService>();

// Application Services
builder.Services.AddSingleton<ITextExtractor, TextExtractor>();
builder.Services.AddSingleton<IChunkingService, ChunkingService>();
builder.Services.AddSingleton<IngestionQueue>();
builder.Services.AddScoped<IIngestionService, IngestionService>();
builder.Services.AddScoped<IRagService, RagService>();

// Background Worker
builder.Services.AddHostedService<IngestionWorker>();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Document Q&A API", Version = "v1" });
});

var app = builder.Build();

// Middleware
app.UseSwagger();
app.UseSwaggerUI();

// Endpoints
app.MapDocumentEndpoints();
app.MapChatEndpoints();

// Health check
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow }));

app.Run();
