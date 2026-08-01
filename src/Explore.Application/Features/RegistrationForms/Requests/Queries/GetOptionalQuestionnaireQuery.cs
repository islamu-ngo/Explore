// ABOUTME: Defines the anonymous event-scoped optional-questionnaire descriptor read.
// ABOUTME: Returns no resource unless an active walk-in standalone attachment resolves to a published version.

using Explore.Application.DTOs.RegistrationForms;
using MediatR;

namespace Explore.Application.Features.RegistrationForms.Requests.Queries;

public sealed record GetOptionalQuestionnaireQuery(Guid EventId) : IRequest<OptionalQuestionnaireDto?>;
