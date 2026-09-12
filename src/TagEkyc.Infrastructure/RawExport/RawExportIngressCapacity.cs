using TagEkyc.Application.Ports;

namespace TagEkyc.Infrastructure.RawExport;

public sealed class RawExportIngressCapacity(
    int maximumConcurrentStreamsPerProducer,
    int maximumConcurrentStreamsPerDeployment,
    long maximumAggregatePlaintextBytesPerDeployment,
    long maximumPlaintextBytesPerStream) : IRawExportIngressCapacity
{
    private readonly object gate = new();
    private readonly Dictionary<string, int> producerCounts = new(StringComparer.Ordinal);
    private int streamCount;
    private long byteCount;

    public IRawExportIngressCapacityLease? TryAcquire(string producerId, long plaintextBytes)
    {
        if (string.IsNullOrWhiteSpace(producerId) || plaintextBytes <= 0 ||
            plaintextBytes > maximumPlaintextBytesPerStream)
        {
            return null;
        }

        lock (gate)
        {
            producerCounts.TryGetValue(producerId, out var producerCount);
            if (producerCount >= maximumConcurrentStreamsPerProducer ||
                streamCount >= maximumConcurrentStreamsPerDeployment ||
                plaintextBytes > maximumAggregatePlaintextBytesPerDeployment - byteCount)
            {
                return null;
            }

            producerCounts[producerId] = producerCount + 1;
            streamCount++;
            byteCount += plaintextBytes;
            return new Lease(this, producerId, plaintextBytes);
        }
    }

    private void Release(string producerId, long plaintextBytes)
    {
        lock (gate)
        {
            if (--producerCounts[producerId] == 0) producerCounts.Remove(producerId);
            streamCount--;
            byteCount -= plaintextBytes;
        }
    }

    private sealed class Lease(RawExportIngressCapacity owner, string producerId, long bytes)
        : IRawExportIngressCapacityLease
    {
        private RawExportIngressCapacity? current = owner;

        public void Dispose() => Interlocked.Exchange(ref current, null)?.Release(producerId, bytes);
    }
}
