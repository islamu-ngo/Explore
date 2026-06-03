// ABOUTME: Application command for running tenant-scoped AI assistant retention cleanup.
// ABOUTME: Supports dry-run operator checks while using tenant settings for retention age.

using Explore.Application.Models;
using MediatR;

namespace Explore.Application.Features.AiAssistant.Requests.Commands;

public sealed class RunAiRetentionCleanupCommand : IRequest<AiRetentionCleanupResult>
{
    public bool DryRun { get; set; }
    public DateTime? UtcNow { get; set; }
}
