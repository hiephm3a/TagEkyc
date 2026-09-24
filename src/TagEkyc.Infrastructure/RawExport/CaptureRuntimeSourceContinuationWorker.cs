using Microsoft.Extensions.Logging;

namespace TagEkyc.Infrastructure.RawExport;

// Registered only by the eventual qualified A3 API composition, never the
// metadata broker. This loop has no request token, body or writer entry point.
internal sealed class CaptureRuntimeSourceContinuationWorker(
    CaptureRuntimeSourcePipeline pipeline, RawIngressBrokerOptions options,
    ILogger<CaptureRuntimeSourceContinuationWorker> logger)
{
    private int running;
    internal async Task RunAsync(CancellationToken stoppingToken)
    {
        if (Interlocked.CompareExchange(ref running, 1, 0) != 0)
            throw new InvalidOperationException("RAW_INGRESS_CONTINUATION_ALREADY_RUNNING");
        try { await RunLoopAsync(stoppingToken); }
        finally { Volatile.Write(ref running, 0); }
    }

    private async Task RunLoopAsync(CancellationToken stoppingToken)
    {
        Guid? cursor = null;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var page = await pipeline.ScanAsync(cursor, stoppingToken);
                if (page.Count == 0)
                {
                    cursor = null;
                    await Task.Delay(options.ContinuationPollIntervalMilliseconds, stoppingToken);
                    continue;
                }
                foreach (var source in page)
                {
                    stoppingToken.ThrowIfCancellationRequested();
                    try { await pipeline.AdvanceAsync(source, options.RequestTimeoutMilliseconds, stoppingToken); }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { throw; }
                    catch (Exception)
                    {
                        // No source IDs, SQL/provider messages or raw metadata in
                        // logs. Failed/ambiguous work remains durable for a poll.
                        logger.LogWarning("Retained continuation stage deferred.");
                    }
                    cursor = source;
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception)
            {
                logger.LogWarning("Retained continuation scan deferred.");
                await Task.Delay(options.ContinuationPollIntervalMilliseconds, stoppingToken);
            }
        }
    }
}
