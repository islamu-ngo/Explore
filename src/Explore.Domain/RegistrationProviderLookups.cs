// ABOUTME: Normalized lookup rows for registration-provider configuration, mappings, trust, drift, and lifecycle.
// ABOUTME: Provides stable int identities and portable metadata without provider-specific classes.

namespace Explore.Domain;

public sealed class RegistrationProviderKind { public int Id { get; set; } public string MasterCode { get; set; } = string.Empty; public string FullName { get; set; } = string.Empty; public string? Description { get; set; } }
public sealed class RegistrationProviderDeploymentKind { public int Id { get; set; } public string MasterCode { get; set; } = string.Empty; public string FullName { get; set; } = string.Empty; public string? Description { get; set; } }
public sealed class RegistrationProviderSchemaAuthority { public int Id { get; set; } public string MasterCode { get; set; } = string.Empty; public string FullName { get; set; } = string.Empty; public string? Description { get; set; } }
public sealed class RegistrationProviderPresentationMode { public int Id { get; set; } public string MasterCode { get; set; } = string.Empty; public string FullName { get; set; } = string.Empty; public string? Description { get; set; } }
public sealed class RegistrationProviderCollectionMode { public int Id { get; set; } public string MasterCode { get; set; } = string.Empty; public string FullName { get; set; } = string.Empty; public string? Description { get; set; } }
public sealed class RegistrationProviderCompletionMode { public int Id { get; set; } public string MasterCode { get; set; } = string.Empty; public string FullName { get; set; } = string.Empty; public string? Description { get; set; } }
public sealed class RegistrationProviderTrustLevel { public int Id { get; set; } public string MasterCode { get; set; } = string.Empty; public string FullName { get; set; } = string.Empty; public string? Description { get; set; } }
public sealed class RegistrationProviderDriftClass { public int Id { get; set; } public string MasterCode { get; set; } = string.Empty; public string FullName { get; set; } = string.Empty; public string? Description { get; set; } }
public sealed class RegistrationProviderBindingState { public int Id { get; set; } public string MasterCode { get; set; } = string.Empty; public string FullName { get; set; } = string.Empty; public string? Description { get; set; } }
