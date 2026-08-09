// ABOUTME: Covers provider-neutral capability resolution, schema drift classes, and mapping publication commands.
// ABOUTME: Keeps Phase 9 Wave B policy tests in Application without EF, API, or provider adapters.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services.Registration;
using Explore.Application.Features.RegistrationProviders.Commands;
using Explore.Application.Services.Registration;
using Explore.Domain;
using Explore.Domain.Enums;

namespace Event.Application.UnitTests.Features.RegistrationProviders;

public sealed class RegistrationProviderCapabilityResolverTests
{
    private static readonly DateTime Now = new(2026, 8, 9, 12, 0, 0, DateTimeKind.Utc);
    private static readonly RegistrationProviderTuple NativeTuple = new("NATIVE", "NATIVE", "ISLAMU_EVENT", "D3_NATIVE", "BUILTIN");

    [Arguments(RegistrationProviderSchemaDriftClass.NoDrift)]
    [Arguments(RegistrationProviderSchemaDriftClass.AdditiveOptionalChange)]
    [Arguments(RegistrationProviderSchemaDriftClass.LabelOnlyChange)]
    [Test]
    public async Task Resolve_IntersectsProofConfiguredGovernanceMappingDriftAndAuthorization(RegistrationProviderSchemaDriftClass drift)
    {
        RegistrationProviderBinding binding = Binding();
        AddCapabilityTuple(binding, NativeTuple, RegistrationProviderCapabilityCodes.Redirect, RegistrationProviderCapabilityCodes.Manual, RegistrationProviderCapabilityCodes.AutoFinalize);
        RegistrationEffectiveCapabilityResolver resolver = new(new Registry(new Descriptor(NativeTuple, RegistrationProviderCapabilitySet.Native)));

        RegistrationEffectiveCapabilityResult result = resolver.Resolve(binding, Request(NativeTuple, drift));

        await Assert.That(result.TupleKnown).IsTrue();
        await Assert.That(result.RedirectAvailable).IsTrue();
        await Assert.That(result.ManualAvailable).IsTrue();
        await Assert.That(result.AutoFinalizable).IsTrue();
        await Assert.That(result.Blockers).IsEmpty();
    }

    [Test]
    public async Task Resolve_MissingIntersectionDimensionBlocksAutoFinalization()
    {
        RegistrationProviderBinding binding = Binding();
        AddCapabilityTuple(binding, NativeTuple, RegistrationProviderCapabilityCodes.Redirect, RegistrationProviderCapabilityCodes.Manual, RegistrationProviderCapabilityCodes.AutoFinalize);
        RegistrationEffectiveCapabilityResolver resolver = new(new Registry(new Descriptor(NativeTuple, RegistrationProviderCapabilitySet.Native)));

        RegistrationEffectiveCapabilityResult mapping = resolver.Resolve(binding, Request(NativeTuple, mappingCompatible: false));
        RegistrationEffectiveCapabilityResult governance = resolver.Resolve(binding, Request(NativeTuple, governance: RegistrationProviderCapabilitySet.None));
        RegistrationEffectiveCapabilityResult auth = resolver.Resolve(binding, Request(NativeTuple, isAuthorized: false));

        await Assert.That(mapping.AutoFinalizable).IsFalse();
        await Assert.That(governance.AutoFinalizable).IsFalse();
        await Assert.That(auth.AutoFinalizable).IsFalse();
        await Assert.That(mapping.Blockers).Contains("mapping_incompatible");
        await Assert.That(auth.Blockers).Contains("authorization_denied");
    }

    [Test]
    public async Task Resolve_UnknownTupleFailsAutoClosedButKeepsRedirectAndManualExplicit()
    {
        RegistrationProviderTuple unknown = new("UNKNOWN", "HOSTED", "v1", "policy", "evidence");
        RegistrationProviderBinding binding = Binding();
        AddCapabilityTuple(binding, unknown, RegistrationProviderCapabilityCodes.Redirect, RegistrationProviderCapabilityCodes.Manual, RegistrationProviderCapabilityCodes.AutoFinalize);
        RegistrationEffectiveCapabilityResolver resolver = new(new Registry(new Descriptor(NativeTuple, RegistrationProviderCapabilitySet.Native)));

        RegistrationEffectiveCapabilityResult result = resolver.Resolve(binding, Request(unknown));

        await Assert.That(result.TupleKnown).IsFalse();
        await Assert.That(result.RedirectAvailable).IsTrue();
        await Assert.That(result.ManualAvailable).IsTrue();
        await Assert.That(result.AutoFinalizable).IsFalse();
        await Assert.That(result.Blockers).Contains("unknown_tuple");
    }

    [Arguments(RegistrationProviderSchemaDriftClass.MappingRequired)]
    [Arguments(RegistrationProviderSchemaDriftClass.RequiredFieldRemoved)]
    [Arguments(RegistrationProviderSchemaDriftClass.TypeChanged)]
    [Arguments(RegistrationProviderSchemaDriftClass.OptionSetChanged)]
    [Arguments(RegistrationProviderSchemaDriftClass.UnsupportedChange)]
    [Test]
    public async Task Resolve_FailClosedDriftBlocksAutoFinalization(RegistrationProviderSchemaDriftClass drift)
    {
        RegistrationProviderBinding binding = Binding();
        AddCapabilityTuple(binding, NativeTuple, RegistrationProviderCapabilityCodes.Redirect, RegistrationProviderCapabilityCodes.Manual, RegistrationProviderCapabilityCodes.AutoFinalize);
        RegistrationEffectiveCapabilityResolver resolver = new(new Registry(new Descriptor(NativeTuple, RegistrationProviderCapabilitySet.Native)));

        RegistrationEffectiveCapabilityResult result = resolver.Resolve(binding, Request(NativeTuple, drift));

        await Assert.That(result.AutoFinalizable).IsFalse();
        await Assert.That(result.Blockers).Contains("blocking_drift");
    }

    [Test]
    public async Task SchemaDriftClassifier_ReturnsEveryRequiredClass()
    {
        SchemaDriftClassifier classifier = new();
        RegistrationProviderSchemaSnapshot baseline = Snapshot(Field("email", "Email", "string", true, [Option("yes", "Yes")]));

        await Assert.That(classifier.Classify(baseline, baseline)).IsEqualTo(RegistrationProviderSchemaDriftClass.NoDrift);
        await Assert.That(classifier.Classify(baseline, Snapshot(Field("email", "Email", "string", true, [Option("yes", "Yes")]), Field("note", "Note", "string", false)))).IsEqualTo(RegistrationProviderSchemaDriftClass.AdditiveOptionalChange);
        await Assert.That(classifier.Classify(baseline, Snapshot(Field("email", "E-mail", "string", true, [Option("yes", "Yep")])))).IsEqualTo(RegistrationProviderSchemaDriftClass.LabelOnlyChange);
        await Assert.That(classifier.Classify(baseline, Snapshot(Field("note", "Note", "string", false)))).IsEqualTo(RegistrationProviderSchemaDriftClass.RequiredFieldRemoved);
        await Assert.That(classifier.Classify(Snapshot(Field("note", "Note", "string", false)), Snapshot(Field("other", "Other", "string", false)))).IsEqualTo(RegistrationProviderSchemaDriftClass.MappingRequired);
        await Assert.That(classifier.Classify(Snapshot(Field("email", "Email", "string", false)), baseline)).IsEqualTo(RegistrationProviderSchemaDriftClass.MappingRequired);
        await Assert.That(classifier.Classify(baseline, Snapshot(Field("email", "Email", "string", false, [Option("yes", "Yes")])))).IsEqualTo(RegistrationProviderSchemaDriftClass.AdditiveOptionalChange);
        await Assert.That(classifier.Classify(baseline, Snapshot(Field("email", "Email", "number", true, [Option("yes", "Yes")])))).IsEqualTo(RegistrationProviderSchemaDriftClass.TypeChanged);
        await Assert.That(classifier.Classify(baseline, Snapshot(Field("email", "Email", "string", true, [Option("no", "No")])))).IsEqualTo(RegistrationProviderSchemaDriftClass.OptionSetChanged);
        await Assert.That(classifier.Classify(Snapshot(Field("dup", "A", "string", false), Field("dup", "B", "string", false)), baseline)).IsEqualTo(RegistrationProviderSchemaDriftClass.UnsupportedChange);
    }

    [Test]
    public async Task SchemaDriftClassifier_UsesBlockingPrecedenceBeforeLabelAndOptionalChanges()
    {
        SchemaDriftClassifier classifier = new();
        RegistrationProviderSchemaSnapshot baseline = Snapshot(Field("email", "Email", "string", false, [Option("yes", "Yes")]));

        await Assert.That(classifier.Classify(baseline, Snapshot(Field("email", "E-mail", "string", true, [Option("yes", "Yep")]))))
            .IsEqualTo(RegistrationProviderSchemaDriftClass.MappingRequired);
        await Assert.That(classifier.Classify(baseline, Snapshot(Field("email", "E-mail", "number", true, [Option("no", "No")]))))
            .IsEqualTo(RegistrationProviderSchemaDriftClass.TypeChanged);
    }

    [Test]
    public async Task PublishCommand_RefusesBlockingDriftAndPropagatesCancellation()
    {
        RegistrationProviderBinding binding = Binding();
        FakeProviderRepository repository = new(binding);
        PublishRegistrationProviderBindingCommandHandler handler = new(repository);
        using CancellationTokenSource source = new();
        await source.CancelAsync();

        await Assert.That(() => handler.Handle(new(binding.TenantId, binding.Id, RegistrationProviderSchemaDriftClass.NoDrift, Now), source.Token))
            .Throws<OperationCanceledException>();

        var blocked = await handler.Handle(new(binding.TenantId, binding.Id, RegistrationProviderSchemaDriftClass.TypeChanged, Now), CancellationToken.None);
        await Assert.That(blocked.Success).IsFalse();
        await Assert.That(blocked.FailureCode).IsEqualTo("registration_provider_drift_blocks_publication");
    }

    [Test]
    public async Task PublishCommand_ComputesDeterministicMappingHashFromPersistedMappings()
    {
        RegistrationProviderBinding first = Binding();
        RegistrationProviderBinding second = Binding(formVersionId: first.RegistrationFormVersionId);
        ReplaceDraftRegistrationProviderMappingsCommandHandler firstReplace = new(new FakeProviderRepository(first));
        ReplaceDraftRegistrationProviderMappingsCommandHandler secondReplace = new(new FakeProviderRepository(second));

        await firstReplace.Handle(new(first.TenantId, first.Id,
            [new("name", "full_name", true), new("email", "email", true)],
            [new("name", "legal", "full"), new("email", "primary", "main")]), CancellationToken.None);
        await secondReplace.Handle(new(second.TenantId, second.Id,
            [new("email", "email", true), new("name", "full_name", true)],
            [new("email", "primary", "main"), new("name", "legal", "full")]), CancellationToken.None);

        await new PublishRegistrationProviderBindingCommandHandler(new FakeProviderRepository(first))
            .Handle(new(first.TenantId, first.Id, RegistrationProviderSchemaDriftClass.NoDrift, Now), CancellationToken.None);
        await new PublishRegistrationProviderBindingCommandHandler(new FakeProviderRepository(second))
            .Handle(new(second.TenantId, second.Id, RegistrationProviderSchemaDriftClass.NoDrift, Now), CancellationToken.None);

        await Assert.That(first.PublishedMappingRevisionHash).IsNotNull();
        await Assert.That(first.PublishedMappingRevisionHash).IsEqualTo(second.PublishedMappingRevisionHash);
    }

    [Test]
    public async Task ReplaceMappings_RefusesPinnedBindingAfterSubmissions()
    {
        RegistrationProviderBinding binding = Binding();
        FakeProviderRepository repository = new(binding) { HasSubmission = true };
        ReplaceDraftRegistrationProviderMappingsCommandHandler handler = new(repository);

        var result = await handler.Handle(new(binding.TenantId, binding.Id,
            [new("attendee.email", "email", true)], []), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("registration_provider_mapping_pinned");
    }

    [Arguments("registration_provider_duplicate_field_mapping")]
    [Arguments("registration_provider_duplicate_provider_field_mapping")]
    [Arguments("registration_provider_option_field_not_found")]
    [Arguments("registration_provider_duplicate_option_mapping")]
    [Test]
    public async Task ReplaceMappings_PrevalidatesBadInputWithoutPartialMutation(string expectedFailureCode)
    {
        RegistrationProviderBinding binding = Binding();
        FakeProviderRepository repository = new(binding);
        ReplaceDraftRegistrationProviderMappingsCommandHandler handler = new(repository);
        ReplaceDraftRegistrationProviderMappingsCommand command = expectedFailureCode switch
        {
            "registration_provider_duplicate_field_mapping" => new(binding.TenantId, binding.Id,
                [new("email", "email", true), new("email", "email2", true)], []),
            "registration_provider_duplicate_provider_field_mapping" => new(binding.TenantId, binding.Id,
                [new("email", " email ", true), new("name", "email", true)], []),
            "registration_provider_option_field_not_found" => new(binding.TenantId, binding.Id,
                [new("email", "email", true)], [new("missing", "yes", "1")]),
            _ => new(binding.TenantId, binding.Id,
                [new("email", "email", true)], [new("email", "yes", "1"), new(" email ", "yes", "2")])
        };

        var result = await handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo(expectedFailureCode);
        await Assert.That(binding.FieldMappings).IsEmpty();
        await Assert.That(binding.OptionMappings).IsEmpty();
        await Assert.That(repository.SaveCount).IsEqualTo(0);
    }

    private static RegistrationEffectiveCapabilityRequest Request(
        RegistrationProviderTuple tuple,
        RegistrationProviderSchemaDriftClass drift = RegistrationProviderSchemaDriftClass.NoDrift,
        RegistrationProviderCapabilitySet? governance = null,
        bool mappingCompatible = true,
        bool isAuthorized = true) => new(Guid.CreateVersion7(), Guid.CreateVersion7(), tuple,
        governance ?? RegistrationProviderCapabilitySet.Native,
        RegistrationProviderCapabilitySet.Native,
        drift,
        mappingCompatible,
        isAuthorized);

    private static RegistrationProviderBinding Binding(Guid? formVersionId = null) => RegistrationProviderBinding.Create(
        Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), formVersionId ?? Guid.CreateVersion7(),
        RegistrationProviderPresentationModeEnum.Redirect, RegistrationProviderCollectionModeEnum.ProviderHosted,
        RegistrationProviderCompletionModeEnum.Callback, RegistrationProviderTrustLevelEnum.FullCanonical, Now);

    private static void AddCapabilityTuple(RegistrationProviderBinding binding, RegistrationProviderTuple tuple, params string[] codes)
    {
        foreach (string code in codes)
        {
            binding.AddCapability(RegistrationProviderCapability.Create(binding, tuple.ProviderCode, tuple.DeploymentKind,
                tuple.ApiVersion, tuple.AdapterPolicyVersion, tuple.ConformanceEvidenceRevision, code));
        }
    }

    private static RegistrationProviderSchemaSnapshot Snapshot(params RegistrationProviderSchemaFieldSnapshot[] fields) => new(fields);
    private static RegistrationProviderSchemaFieldSnapshot Field(string key, string label, string type, bool required, IReadOnlyList<RegistrationProviderSchemaOptionSnapshot>? options = null) => new(key, label, type, required, options ?? []);
    private static RegistrationProviderSchemaOptionSnapshot Option(string key, string label) => new(key, label);
    private static RegistrationEvidenceHash Hash() => RegistrationEvidenceHash.Create(Convert.ToBase64String(new byte[32]));

    private sealed record Descriptor(RegistrationProviderTuple Tuple, RegistrationProviderCapabilitySet ProvenCapabilities) : IRegistrationProviderDescriptor;

    private sealed class Registry(params IRegistrationProviderDescriptor[] descriptors) : IRegistrationProviderRegistry
    {
        public IRegistrationProviderDescriptor? TryResolve(RegistrationProviderTuple tuple) => descriptors.SingleOrDefault(descriptor => descriptor.Tuple == tuple);
    }

    private sealed class FakeProviderRepository(RegistrationProviderBinding binding) : IRegistrationProviderRepository
    {
        public bool HasSubmission { get; init; }
        public int SaveCount { get; private set; }
        public Task<RegistrationProviderConnection?> GetConnectionAsync(Guid tenantId, Guid connectionId, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<RegistrationProviderBinding?> GetBindingAsync(Guid tenantId, Guid bindingId, CancellationToken cancellationToken) => cancellationToken.IsCancellationRequested ? Task.FromCanceled<RegistrationProviderBinding?>(cancellationToken) : Task.FromResult<RegistrationProviderBinding?>(binding);
        public Task<RegistrationProviderBinding?> GetBindingForCallbackAsync(Guid bindingId, CancellationToken cancellationToken) => Task.FromResult<RegistrationProviderBinding?>(binding.Id == bindingId ? binding : null);
        public Task<bool> HasSubmissionForBindingAsync(Guid tenantId, Guid bindingId, CancellationToken cancellationToken) => Task.FromResult(HasSubmission);
        public Task AddConnectionAsync(RegistrationProviderConnection connection, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task AddBindingAsync(RegistrationProviderBinding binding, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task AddSchemaRevisionAsync(RegistrationProviderSchemaRevision revision, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task SaveChangesAsync(CancellationToken cancellationToken) { SaveCount++; return Task.CompletedTask; }
    }
}
