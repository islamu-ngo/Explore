// ABOUTME: Exposes deterministic terminal coordination internals only to the owning focused test assembly.
// ABOUTME: Keeps production key/signal tests independent of real terminals, timing, and operating-system signals.

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Event.SetupAssistant.Cli.Tests")]
