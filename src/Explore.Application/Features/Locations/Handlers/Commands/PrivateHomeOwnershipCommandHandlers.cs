// ABOUTME: Handlers that bind Private Home classification and ownership to explicit, versioned consent.
// ABOUTME: Never infer ownership from CreatedBy; the acting user must acknowledge the household statement.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Exceptions;
using Explore.Application.Features.Locations.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using MediatR;

namespace Explore.Application.Features.Locations.Handlers.Commands;

public sealed class ClassifyLocationAsPrivateHomeCommandHandler(
    ILocationRepository locations,
    ICurrentUserService currentUser)
    : IRequestHandler<ClassifyLocationAsPrivateHomeCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(
        ClassifyLocationAsPrivateHomeCommand request,
        CancellationToken cancellationToken)
    {
        if (PrivateHomeConsent.Validate(request.ConsentAcknowledged, request.ConsentVersion)
            is { } consentFailure)
        {
            return consentFailure;
        }

        if (currentUser.UserId is not { } actorUserId || actorUserId == Guid.Empty)
        {
            return PrivateHomeConsent.AuthenticationRequired();
        }

        Location? location = await locations.GetById(request.LocationId);
        if (location is null)
        {
            return PrivateHomeConsent.NotFound();
        }

        PrivateHomeConsent.RequireCurrentStamp(location, request.ExpectedConcurrencyStamp);

        try
        {
            location.ClassifyAsPrivateHome(actorUserId);
        }
        catch (InvalidOperationException exception)
        {
            // The aggregate refuses to move ownership without the new owner's own consent.
            return PrivateHomeConsent.Rejected(exception.Message);
        }

        await locations.Update(location);
        return new BaseCommandResponse<Guid>
        {
            Success = true,
            Id = location.Id,
            Message = "Location classified as a private home."
        };
    }
}

public sealed class AcceptPrivateHomeOwnershipCommandHandler(
    ILocationRepository locations,
    ICurrentUserService currentUser,
    TimeProvider timeProvider)
    : IRequestHandler<AcceptPrivateHomeOwnershipCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(
        AcceptPrivateHomeOwnershipCommand request,
        CancellationToken cancellationToken)
    {
        if (PrivateHomeConsent.Validate(request.ConsentAcknowledged, request.ConsentVersion)
            is { } consentFailure)
        {
            return consentFailure;
        }

        if (currentUser.UserId is not { } actorUserId || actorUserId == Guid.Empty)
        {
            return PrivateHomeConsent.AuthenticationRequired();
        }

        Location? location = await locations.GetById(request.LocationId);
        if (location is null)
        {
            return PrivateHomeConsent.NotFound();
        }

        PrivateHomeConsent.RequireCurrentStamp(location, request.ExpectedConcurrencyStamp);

        try
        {
            // Consenting user and new owner are the same person by construction, which is exactly the
            // invariant the aggregate enforces.
            location.TransferPrivateHomeOwnership(new LocationOwnershipConsent(
                actorUserId,
                actorUserId,
                timeProvider.GetUtcNow().UtcDateTime,
                request.ConsentVersion.Trim()));
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
        {
            return PrivateHomeConsent.Rejected(exception.Message);
        }

        await locations.Update(location);
        return new BaseCommandResponse<Guid>
        {
            Success = true,
            Id = location.Id,
            Message = "Private home ownership accepted."
        };
    }
}

internal static class PrivateHomeConsent
{
    public const string ConsentRequiredFailureCode = "private_home_consent_required";
    public const string RejectedFailureCode = "private_home_ownership_rejected";

    public static BaseCommandResponse<Guid>? Validate(bool acknowledged, string? consentVersion)
    {
        if (acknowledged && !string.IsNullOrWhiteSpace(consentVersion))
        {
            return null;
        }

        return new BaseCommandResponse<Guid>
        {
            Success = false,
            Message = "Private home ownership requires an explicit, versioned consent acknowledgement.",
            FailureCode = ConsentRequiredFailureCode,
            Errors = ["Explicit household consent is required before a location becomes a private home."]
        };
    }

    public static BaseCommandResponse<Guid> AuthenticationRequired() => new()
    {
        Success = false,
        Message = "An authenticated owner is required.",
        FailureCode = FailureCodes.AuthenticationRequired
    };

    public static BaseCommandResponse<Guid> NotFound() => new()
    {
        Success = false,
        Message = "Location not found.",
        FailureCode = FailureCodes.NotFound
    };

    public static BaseCommandResponse<Guid> Rejected(string message) => new()
    {
        Success = false,
        Message = message,
        FailureCode = RejectedFailureCode,
        Errors = [message]
    };

    public static void RequireCurrentStamp(Location location, Guid expectedConcurrencyStamp)
    {
        if (location.ConcurrencyStamp != expectedConcurrencyStamp)
        {
            throw new ConcurrencyConflictException(
                ConcurrencyConflictException.ConcurrentUpdate,
                "The location was modified by another request. Reload and retry.",
                nameof(Location),
                location.Id.ToString());
        }
    }
}
