// ABOUTME: Validates identifiers for separately authorized webhook payload reads.
// ABOUTME: Prevents invalid identifiers from reaching tenant-scoped persistence or audit boundaries.

using Explore.Application.Features.Webhooks.Requests.Queries;
using FluentValidation;

namespace Explore.Application.Features.Webhooks.Validators;

public sealed class GetWebhookMessagePayloadQueryValidator : AbstractValidator<GetWebhookMessagePayloadQuery>
{
    public GetWebhookMessagePayloadQueryValidator()
    {
        RuleFor(query => query.MessageId).NotEmpty();
    }
}
