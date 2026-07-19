// ABOUTME: Enforces a bounded offset window for merged local and ATProto public event discovery.
// ABOUTME: Prevents later-page requests from creating unbounded top-window allocations.

using Explore.Application.Features.Federation.Atproto.Requests.Queries;
using FluentValidation;

namespace Explore.Application.Features.Federation.Atproto.Validators;

public sealed class GetPublicEventDiscoveryRequestValidator : AbstractValidator<GetPublicEventDiscoveryRequest>
{
    public const int MaximumWindowSize = 1000;

    public GetPublicEventDiscoveryRequestValidator()
    {
        RuleFor(request => request.Criteria.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(request => request.Criteria.PageSize).InclusiveBetween(1, 100);
        RuleFor(request => request).Must(FitsBoundedWindow)
            .WithMessage($"The public event discovery window cannot exceed {MaximumWindowSize} items.");
    }

    public static bool TryGetWindow(GetPublicEventDiscoveryRequest request, out int window)
    {
        try
        {
            window = checked((request.Criteria.PageNumber - 1) * request.Criteria.PageSize + request.Criteria.PageSize);
            return window is > 0 and <= MaximumWindowSize;
        }
        catch (OverflowException)
        {
            window = 0;
            return false;
        }
    }

    private static bool FitsBoundedWindow(GetPublicEventDiscoveryRequest request) =>
        TryGetWindow(request, out _);
}
