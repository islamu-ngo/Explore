// ABOUTME: Publishes a form version by generating and pinning its canonical schema artifact bundle.
// ABOUTME: Keeps artifact bytes and hashes Application-owned so callers cannot supply publication content.

using Explore.Application.Contracts.Services;
using Explore.Domain;

namespace Explore.Application.Services.Registration;

public sealed class FormSchemaArtifactPublicationService(IFormSchemaArtifactGenerator generator)
{
    public void Publish(RegistrationFormVersion version, DateTime publishedAt)
    {
        ArgumentNullException.ThrowIfNull(version);
        FormSchemaArtifactBundle artifacts = generator.Generate(version);
        version.PinGeneratedSchemaBundle(artifacts.CanonicalBundleJson, publishedAt);
    }
}
