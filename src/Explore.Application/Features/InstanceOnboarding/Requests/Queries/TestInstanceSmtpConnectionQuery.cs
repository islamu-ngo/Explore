// ABOUTME: MediatR query for testing the configured instance email provider connection.
// ABOUTME: Returns a safe Application-owned diagnostic result for API mapping.

using Explore.Application.Models;
using MediatR;

namespace Explore.Application.Features.InstanceOnboarding.Requests.Queries;

public sealed record TestInstanceSmtpConnectionQuery : IRequest<EmailResult>
{
}
