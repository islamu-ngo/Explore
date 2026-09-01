// ABOUTME: Exposes deterministic CLI parsing internals only to the owning focused test assembly.
// ABOUTME: Keeps command-contract tests independent of ambient streams and filesystem access.

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Event.SetupAssistant.Cli.Tests")]
