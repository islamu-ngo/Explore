// ABOUTME: Narrow seam over the CarpaNet Jetstream v2 sealed-archive endpoints used by recovery scoping.
// ABOUTME: Exposes plan and decoded-block reads so probe branch logic is testable without network access.

using CarpaNet.Jetstream;
using Microsoft.Extensions.Options;

namespace Explore.Infrastructure.Services.Federation;

/// <summary>
/// The two archive operations recovery scoping needs. Block download and frame decoding are deliberately
/// one operation: the wire format is an implementation detail, and collapsing them keeps test doubles from
/// having to synthesise binary segment frames.
/// </summary>
internal interface IAtprotoJetstreamArchiveClient
{
    Task<JetstreamSnapshotPlan> PlanSnapshotAsync(
        JetstreamSnapshotPlanRequest request,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<JetstreamSegmentRow>> GetBlockRowsAsync(
        string segmentName,
        int blockIndex,
        CancellationToken cancellationToken);
}

internal sealed class CarpaNetJetstreamArchiveClient : IAtprotoJetstreamArchiveClient, IDisposable
{
    private readonly JetstreamV2Client _client;
    private int _disposed;

    public CarpaNetJetstreamArchiveClient(IOptions<AtprotoJetstreamOptions> options)
    {
        AtprotoJetstreamOptions configured = options.Value;
        _client = new JetstreamV2Client(
            new Uri(configured.Endpoint, UriKind.Absolute),
            new JetstreamV2ClientOptions
            {
                EnableCompression = configured.EnableCompression,
                DownloadConcurrency = configured.ArchiveProbeDownloadConcurrency
            });
    }

    public Task<JetstreamSnapshotPlan> PlanSnapshotAsync(
        JetstreamSnapshotPlanRequest request,
        CancellationToken cancellationToken) =>
        _client.PlanSnapshotAsync(request, cancellationToken);

    public async Task<IReadOnlyList<JetstreamSegmentRow>> GetBlockRowsAsync(
        string segmentName,
        int blockIndex,
        CancellationToken cancellationToken)
    {
        byte[] frame = await _client.GetBlockAsync(segmentName, blockIndex, cancellationToken);
        return JetstreamSegmentFormat.DecodeBlockFrame(frame);
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            _client.Dispose();
        }
    }
}
