// ABOUTME: Grants the Application layer the internal aggregate pinning seam for generated form artifacts.
// ABOUTME: Grants Domain and Application tests narrow internal construction access without public exposure.

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Explore.Application")]
[assembly: InternalsVisibleTo("Explore.Persistence")]
[assembly: InternalsVisibleTo("Event.Domain.UnitTests")]
[assembly: InternalsVisibleTo("Event.Application.UnitTests")]
[assembly: InternalsVisibleTo("Event.Persistence.IntegrationTests")]
