// ABOUTME: Declares test-only access to internal persistence seams.
// ABOUTME: Keeps deterministic unit-of-work retry tests out of the production API surface.

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Event.Persistence.IntegrationTests")]
