// ABOUTME: Grants the Application layer the internal aggregate pinning seam for generated form artifacts.
// ABOUTME: Grants Domain tests direct access to malformed-bundle atomicity checks without public exposure.

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Explore.Application")]
[assembly: InternalsVisibleTo("Event.Domain.UnitTests")]
