// ABOUTME: Scoped aggregates/redactions that instance-admin AI flows may consume (CTO correction #1).
// ABOUTME: Never authorizes row-level user PII through the general AI assistant.

namespace Explore.Domain.Enums;

public enum AiAdministrativeContextScopeEnum
{
    InstanceAggregate = 0,
    TenantAggregate = 1,
    OperationalDiagnostics = 2
}
