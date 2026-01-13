using DocumentQA.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DocumentQA.Application.BackgroundJobs;

public class IngestionWorker : BackgroundService
{
    private readonly IngestionQueue _queue;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<IngestionWorker> _logger;

    public IngestionWorker(
        IngestionQueue queue,
        IServiceProvider serviceProvider,
        ILogger<IngestionWorker> logger)
    {
        _queue = queue;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Ingestion worker started");

        await foreach (var job in _queue.DequeueAllAsync(stoppingToken))
        {
            try
            {
                _logger.LogInformation("Processing document {DocumentId}", job.DocumentId);

                // Create a scope to resolve scoped services
                using var scope = _serviceProvider.CreateScope();
                var ingestionService = scope.ServiceProvider.GetRequiredService<IIngestionService>();

                await ingestionService.ProcessDocumentAsync(job, stoppingToken);
                _logger.LogInformation("Completed processing document {DocumentId}", job.DocumentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process document {DocumentId}", job.DocumentId);
            }
        }
    }
}
