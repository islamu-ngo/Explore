// ABOUTME: Defines bounded CSV-import contracts for organizer-managed company registration assignments.
// ABOUTME: Keeps participant PII in input-only rows and returns only aggregate counts plus order id.

namespace Explore.Application.DTOs.RegistrationOrders;

public sealed record CompanyRegistrationAssignmentCsvInputDto(string CsvUtf8, string LineageKey);

public sealed record CompanyRegistrationAssignmentCsvResultDto(Guid RegistrationOrderId, int AssignmentCount, bool AlreadyApplied);
