// ABOUTME: MediatR query for fetching a user's own contact-sharing consents.
// ABOUTME: Powers the Connected Apps / Third-party Access page in user account settings.

using Explore.Application.DTOs.ContactShareConsent;
using MediatR;

namespace Explore.Application.Features.ContactShareConsents.Requests.Queries;

public sealed record GetUserContactShareConsentsQuery(
    Guid UserId = default,
    Guid TenantId = default
) : IRequest<List<UserContactShareConsentDto>>;
