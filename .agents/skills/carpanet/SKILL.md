---
name: carpanet
description: "Load when building .NET ATProtocol applications with CarpaNet source generators, XRPC queries, Jetstream subscriptions, OAuth 2.0 DPoP, or Lexicon JSON models."
type: reference
enforcement: inform
priority: medium
---
<!-- ABOUTME: Compact router for CarpaNet ATProtocol and Roslyn source generator development. -->
<!-- ABOUTME: Guides lexicon resolution, XRPC client instantiation, Jetstream, and DPoP authentication. -->

# CarpaNet ATProtocol Development Router

## Must-Read Docs
- [resources/guide.md](resources/guide.md)

## Top 5 Invariants
1. Declare required lexicons in `.csproj` using `<LexiconResolve Include="..."/>` or `<LexiconFiles Include="..."/>` to drive compile-time source generation.
2. Use source-generated `ATProtoClientFactory.Create()` or explicit `ATProtoJsonContext` and `ATProtoCborContext` instances to ensure reflection-free NativeAOT compatibility.
3. Use `JetstreamV2Client` with `JetstreamV2SubscribeOptions` for live event ingestion, and persist `evt.Seq` for deterministic resume across restarts.
4. For user-facing authentication, use `OAuthSession` with `IOAuthSessionStore` to manage DPoP keys and token lifecycle securely.
5. All ATProtocol records and parameters use strongly-typed identifiers (`ATDid`, `ATHandle`, `ATUri`, `ATIdentifier`) with implicit string conversions.

## Top 5 Anti-Patterns
1. Assuming all Bluesky lexicons are present without explicitly declaring them in the project MSBuild file.
2. Relying on reflection-based `System.Text.Json` serialization instead of the source-generated contexts.
3. Swallowing `ATProtoException` errors or using legacy event callbacks instead of `IAsyncEnumerable` streaming.
4. Committing plain user passwords instead of utilizing App Passwords or OAuth 2.0 DPoP authorization.
5. Hardcoding relay or PDS URLs instead of resolving them via `IdentityResolver`.

## Minimal Examples
```csharp
// Unauthenticated public query
var client = ATProtoClientFactory.Create();
var profile = await client.AppBskyActorGetProfileAsync(
    new AppBsky.Actor.GetProfileParameters { Actor = new ATHandle("alice.bsky.social") });
```

```csharp
// Real-time Jetstream v2 subscription
using var jetstream = new JetstreamV2Client(new Uri(BlueskyServices.JetstreamUsEast));
await foreach (var evt in jetstream.SubscribeAsync(new JetstreamV2SubscribeOptions
{
    Collections = ["app.bsky.feed.post"]
}))
{
    Console.WriteLine($"seq={evt.Seq} {evt.Commit?.Collection}/{evt.Commit?.Rkey}");
}
```

## Verification Hooks
- `dotnet build --configuration Release --verbosity quiet`
- `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

## Related Skills
- [../agentic-research/SKILL.md](../agentic-research/SKILL.md)
