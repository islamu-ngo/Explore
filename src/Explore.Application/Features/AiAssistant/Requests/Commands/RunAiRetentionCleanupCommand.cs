// ABOUTME: Application command for running tenant-scoped AI assistant retention cleanup.
// ABOUTME: Supports dry-run operator checks while using tenant settings for retention age.

using Explore.Application.Models;
using MediatR;

namespace Explore.Application.Features.AiAssistant.Requests.Commands;

public sealed record RunAiRetentionCleanupCommand : IRequest<AiRetentionCleanupResult>
{
    public bool DryRun { get; init; }
    public DateTime? UtcNow { get; init; }
}
