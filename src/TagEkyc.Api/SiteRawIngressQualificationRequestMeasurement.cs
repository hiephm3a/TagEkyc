using TagEkyc.Application.CaptureRuntime;

namespace TagEkyc.Api;

internal sealed class SiteRawIngressQualificationRequestMeasurement
    : ISiteRawIngressQualificationRequestMeasurement
{
    private int brokerCommitted;
    private int readsWhileHeld;

    public Guid? QualificationRunId { get; private set; }
    public bool BrokerCommitted => Volatile.Read(ref brokerCommitted) != 0;
    public int BodyReadsWhileBrokerHeld => Volatile.Read(ref readsWhileHeld);

    public void Begin(Guid qualificationRunId)
    {
        if (qualificationRunId == Guid.Empty || QualificationRunId is not null)
            throw new InvalidOperationException("SITE_QUALIFICATION_REQUEST_STATE_INVALID");
        QualificationRunId = qualificationRunId;
    }

    public void MarkBrokerCommitted()
    {
        if (QualificationRunId is null)
            throw new InvalidOperationException("SITE_QUALIFICATION_REQUEST_STATE_INVALID");
        Interlocked.Exchange(ref brokerCommitted, 1);
    }

    public void ObserveBodyRead()
    {
        if (!BrokerCommitted) Interlocked.Increment(ref readsWhileHeld);
    }
}

internal sealed class SiteRawIngressQualificationBodyStream(
    Stream inner,
    ISiteRawIngressQualificationRequestMeasurement measurement) : Stream
{
    public override bool CanRead => inner.CanRead;
    public override bool CanSeek => inner.CanSeek;
    public override bool CanWrite => false;
    public override long Length => inner.Length;
    public override long Position { get => inner.Position; set => inner.Position = value; }
    public override void Flush() => inner.Flush();
    public override Task FlushAsync(CancellationToken cancellationToken) => inner.FlushAsync(cancellationToken);
    public override int Read(byte[] buffer, int offset, int count)
    {
        measurement.ObserveBodyRead();
        return inner.Read(buffer, offset, count);
    }
    public override int Read(Span<byte> buffer)
    {
        measurement.ObserveBodyRead();
        return inner.Read(buffer);
    }
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer,
        CancellationToken cancellationToken = default)
    {
        measurement.ObserveBodyRead();
        return await inner.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
    }
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count,
        CancellationToken cancellationToken)
    {
        measurement.ObserveBodyRead();
        return inner.ReadAsync(buffer, offset, count, cancellationToken);
    }
    public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
