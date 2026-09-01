// ABOUTME: Exposes target-owned security and binding internals only to the focused Terminal test assembly.
// ABOUTME: Keeps secret-boundary tests independent of the real interactive terminal driver and user files.

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Event.SetupAssistant.Terminal.Tests")]
