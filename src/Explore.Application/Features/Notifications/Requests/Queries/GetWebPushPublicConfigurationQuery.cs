// ABOUTME: Query request for browser-safe Web Push configuration.
// ABOUTME: Returns only VAPID public settings needed before explicit browser consent.

using Explore.Application.Models;
using MediatR;

namespace Explore.Application.Features.Notifications.Requests.Queries;

public sealed record GetWebPushPublicConfigurationQuery : IRequest<WebPushPublicConfiguration>;
