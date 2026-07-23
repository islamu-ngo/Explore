// ABOUTME: Maps canonical inbound ATProto event projections into validated tenant-scoped import plans.
// ABOUTME: Persists canonical state, presentations, cursor, and local import intent through one atomic repository call.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.Federation.Atproto.Models;
using Explore.Application.Features.Federation.Atproto.Requests.Commands;
using Explore.Application.Features.Federation.Atproto.Services;
using Explore.Domain;
using FluentValidation;
using MediatR;

namespace Explore.Application.Features.Federation.Atproto.Handlers.Commands;

public sealed class ImportAtprotoFederatedEventCommandHandler(
    IAtprotoJetstreamRepository repository)
    : IRequestHandler<ImportAtprotoFederatedEventCommand, bool>
{
    public async Task<bool> Handle(
        ImportAtprotoFederatedEventCommand request,
        CancellationToken cancellationToken)
    {
        AtprotoJetstreamApplyRequest applyRequest = request.ApplyRequest;
        IReadOnlyList<AtprotoFederatedEventImportPlan> importPlans =
            await BuildImportPlansAsync(applyRequest, cancellationToken);

        return await repository.TryApplyAndAdvanceAsync(
            applyRequest with { EventImports = importPlans },
            cancellationToken);
    }

    private static async Task<IReadOnlyList<AtprotoFederatedEventImportPlan>> BuildImportPlansAsync(
        AtprotoJetstreamApplyRequest applyRequest,
        CancellationToken cancellationToken)
    {
        if (applyRequest.EventProjection is null)
        {
            return [];
        }

        AtprotoRecord record = applyRequest.Record
            ?? throw new ValidationException("An event projection requires a canonical ATProto record.");
        IEnumerable<Guid> tenantIds = applyRequest.Presentations
            .Where(presentation => presentation.IsVisible)
            .Select(presentation => presentation.TenantId);
        return await AtprotoFederatedEventImportPlanFactory.CreateAsync(
            record,
            applyRequest.EventProjection,
            tenantIds,
            cancellationToken);
    }
}
