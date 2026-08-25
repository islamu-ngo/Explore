// ABOUTME: Query for the current actor's active support-access status.
// ABOUTME: Supports UX banners and BFF status checks without trusting browser-side claims.

using Explore.Application.DTOs.SupportAccess;
using MediatR;

namespace Explore.Application.Features.SupportAccess.Requests.Queries;

public sealed record GetCurrentSupportAccessSessionQuery : IRequest<CurrentSupportAccessSessionDto>;
