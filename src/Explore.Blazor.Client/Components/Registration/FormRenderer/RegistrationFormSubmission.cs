// ABOUTME: Carries one ephemeral renderer submission to the registration journey orchestrator.
// ABOUTME: Exposes only visible answered field values so hidden conditional answers cannot be submitted.

namespace Explore.Blazor.Client.Components.Registration.FormRenderer;

public sealed record RegistrationFormSubmission(IReadOnlyDictionary<Guid, object?> Answers);
