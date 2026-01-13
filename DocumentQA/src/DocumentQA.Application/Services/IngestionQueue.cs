using System.Threading.Channels;

namespace DocumentQA.Application.Services;

public class IngestionQueue
{
    private readonly Channel<IngestionJob> _channel;

    public IngestionQueue()
    {
        _channel = Channel.CreateBounded<IngestionJob>(new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.Wait
        });
    }

    public async Task EnqueueAsync(IngestionJob job, CancellationToken ct = default)
    {
        await _channel.Writer.WriteAsync(job, ct);
    }

    public IAsyncEnumerable<IngestionJob> DequeueAllAsync(CancellationToken ct)
    {
        return _channel.Reader.ReadAllAsync(ct);
    }
}
