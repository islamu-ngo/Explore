// ABOUTME: PATCH wrapper DTO for event registration property updates using nullable logical groups.
// ABOUTME: Route ID targets the row; groups express independent registration update intent.

using Explore.Application.Models.Common;

namespace Explore.Application.DTOs.EventRegistration;

public class UpdateEventRegistrationDto
{
    public UpdateEventRegistrationUserDto? User { get; set; }
    public UpdateEventRegistrationSessionDto? Session { get; set; }
    public UpdateEventRegistrationIntentDto? Intent { get; set; }
    public UpdateEventRegistrationApprovalStatusDto? ApprovalStatus { get; set; }
}

public class UpdateEventRegistrationUserDto
{
    public Guid UserId { get; set; }
}

public class UpdateEventRegistrationSessionDto
{
    public Guid EventSessionId { get; set; }
}

public class UpdateEventRegistrationIntentDto
{
    public OptionalUpdate<Guid?> EventRegistrationIntentId { get; set; } = OptionalUpdate<Guid?>.Unspecified();
}

public class UpdateEventRegistrationApprovalStatusDto
{
    public OptionalUpdate<int?> ApprovalStatusId { get; set; } = OptionalUpdate<int?>.Unspecified();
}
