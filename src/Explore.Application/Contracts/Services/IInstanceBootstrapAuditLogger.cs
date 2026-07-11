// ABOUTME: Application contract for emitting first-run setup and bootstrap audit events.
// ABOUTME: Keeps handlers and API filters on a shared non-secret event vocabulary.

using Explore.Application.Onboarding;

namespace Explore.Application.Contracts.Services;

public interface IInstanceBootstrapAuditLogger
{
    void Log(InstanceBootstrapAuditEvent auditEvent);
}
