// ABOUTME: Defines deterministic registration-form schema artifact generation without persistence or provider IO.
// ABOUTME: Returns the four canonical artifacts, their complete bundle, and its lowercase SHA-256 identity.

using Explore.Domain;

namespace Explore.Application.Contracts.Services;

public interface IFormSchemaArtifactGenerator
{
    FormSchemaArtifactBundle Generate(RegistrationFormVersion version);
}

public sealed record FormSchemaArtifactBundle(
    string DataSchemaJson,
    string UiSchemaJson,
    string LogicSchemaJson,
    string MappingArtifactJson,
    string CanonicalBundleJson,
    string SchemaHash);
