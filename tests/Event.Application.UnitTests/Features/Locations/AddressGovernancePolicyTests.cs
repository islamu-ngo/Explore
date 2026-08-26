// ABOUTME: Red behavioral contracts for conservative address policy, manual creation, and promotion.
// ABOUTME: Requires trusted settings and named authorization while preserving privacy, tenancy, and concurrency.

using System.Reflection;
using Explore.Application.Authorization;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Exceptions;
using Explore.Application.Features.Geocoding.Handlers.Commands;
using Explore.Application.Features.Geocoding.Requests.Commands;
using Explore.Application.Features.Geocoding.Validators;
using Explore.Application.Responses;
using Explore.Application.Settings;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.Settings;
using Explore.Domain.ValueObjects;
using NSubstitute;

namespace ApplicationUnitTests.Features.Locations;

public sealed class AddressGovernancePolicyTests
{
    private const string SourceTypeName = "Explore.Domain.Enums.LocationAddressSourceEnum";
    private const string VisibilityTypeName = "Explore.Domain.Enums.LocationAddressVisibilityEnum";
    private const string ResolverTypeName = "Explore.Application.Features.Geocoding.AddressGovernancePolicyResolver";
    private const string RequestTypeName = "Explore.Application.Features.Geocoding.AddressGovernancePolicyRequest";
    private const string PromotionCommandTypeName = "Explore.Application.Features.Geocoding.Requests.Commands.PromoteLocationAddressCommand";
    private const string PromotionHandlerTypeName = "Explore.Application.Features.Geocoding.Handlers.Commands.PromoteLocationAddressCommandHandler";
    private static readonly Guid TenantId = Guid.Parse("019b0000-0001-7000-8000-000000000001");
    private static readonly Guid ActorId = Guid.Parse("019b0000-0001-7000-8000-000000000002");
    private static readonly Guid OrganizationId = Guid.Parse("019b0000-0001-7000-8000-000000000003");

    [Test]
    public async Task SourceAndVisibilityEnumsHaveExactIndependentOneBasedValues()
    {
        Type source = RequiredDomainType(SourceTypeName);
        Type visibility = RequiredDomainType(VisibilityTypeName);

        await AssertEnumAsync(source, new Dictionary<string, int>
        {
            ["UnknownLegacy"] = 1,
            ["Manual"] = 2,
            ["ProviderSelection"] = 3
        });
        await AssertEnumAsync(visibility, new Dictionary<string, int>
        {
            ["Quarantined"] = 1,
            ["CreatorPrivate"] = 2,
            ["OrganizationScoped"] = 3,
            ["TenantApproved"] = 4
        });
        await Assert.That(source).IsNotEqualTo(visibility);
    }

    [Test]
    public async Task NewAggregateDefaultsToUnknownLegacyAndQuarantinedWithoutInventingScope()
    {
        Location location = NewLocation();

        await Assert.That(RequiredProperty(location, "AddressSource").ToString()).IsEqualTo("UnknownLegacy");
        await Assert.That(RequiredProperty(location, "AddressVisibility").ToString()).IsEqualTo("Quarantined");
        await Assert.That(ReadRequiredProperty(location, "AddressOrganizationId")).IsNull();
        await Assert.That(location.CreatedBy).IsNull();
    }

    [Test]
    public async Task SettingDefinitionsDefaultDisabledAndCannotReachUserScope()
    {
        Type definitions = RequiredDomainType("Explore.Domain.Settings.Definitions.AddressGovernanceSettingDefinitions");
        SettingDefinition mode = RequiredDefinition(definitions, "CreationMode");
        SettingDefinition grant = RequiredDefinition(definitions, "OrganizationCreationGrant");

        await Assert.That(mode.DefaultValue).IsEqualTo("\"Disabled\"");
        await Assert.That(mode.MinScope).IsEqualTo(SettingScope.Instance);
        await Assert.That(mode.MaxScope).IsEqualTo(SettingScope.Tenant);
        await Assert.That(mode.IsLockable).IsTrue();
        await Assert.That(grant.DefaultValue).IsEqualTo("false");
        await Assert.That(grant.MinScope).IsEqualTo(SettingScope.Organization);
        await Assert.That(grant.MaxScope).IsEqualTo(SettingScope.Organization);
    }

    [Test]
    public async Task NamedAuthoritiesAreStableMachineContracts()
    {
        Type locations = typeof(AuthorizationActions.Locations);
        await Assert.That(RequiredConstant(locations, "ManageCustomAddresses")).IsEqualTo("manage_custom_addresses");
        await Assert.That(RequiredConstant(locations, "CreateCustomAddress")).IsEqualTo("create_custom_address");
        await Assert.That(RequiredConstant(locations, "ApproveTenantAddress")).IsEqualTo("approve_tenant_address");
    }

    [Test]
    [Arguments(null, true, null, false, null)]
    [Arguments("Malformed", true, null, false, null)]
    [Arguments("Disabled", true, null, false, null)]
    [Arguments("AdminOnly", true, null, true, "manage_custom_addresses")]
    [Arguments("AdminOnly", false, null, false, "manage_custom_addresses")]
    [Arguments("OrganizationGoverned", true, true, true, "create_custom_address")]
    [Arguments("OrganizationGoverned", true, false, false, null)]
    [Arguments("OpenWithModeration", true, null, true, "create")]
    [Arguments("OpenWithModeration", false, null, false, "create")]
    public async Task ResolverCombinesSettingsWithNamedProviderAuthority(
        string? mode,
        bool authorityAllowed,
        bool? organizationGrant,
        bool expectedAllowed,
        string? expectedAction)
    {
        PolicyExecution execution = await ExecutePolicyAsync(
            mode,
            organizationGrant,
            authorityAllowed,
            tenantId: TenantId,
            organizationId: mode == "OrganizationGoverned" ? OrganizationId : null);

        await Assert.That(RequiredBoolean(execution.Decision, "CanCreateManualAddress")).IsEqualTo(expectedAllowed);
        if (expectedAction is null)
        {
            await Assert.That(execution.AuthorizationRequests).IsEmpty();
        }
        else
        {
            await Assert.That(execution.AuthorizationRequests.Any(request => request.Action == expectedAction)).IsTrue();
            await Assert.That(execution.AuthorizationRequests.All(request => request.Tenant?.TenantId == TenantId)).IsTrue();
            await Assert.That(execution.AuthorizationRequests.All(request => request.Tenant?.OrganizationId
                == (mode == "OrganizationGoverned" ? OrganizationId : null))).IsTrue();
        }

        await Assert.That(execution.SettingContexts).IsNotEmpty();
        await Assert.That(execution.SettingContexts.All(context => context.UserId is null)).IsTrue();
    }

    [Test]
    [Arguments(null, null)]
    [Arguments("OrganizationGoverned", null)]
    [Arguments("OrganizationGoverned", "missing")]
    public async Task ResolverMissingTrustedTenantOrOrganizationContextFailsClosed(
        string? mode,
        string? contextCase)
    {
        Guid? tenantId = contextCase == "missing" ? TenantId : null;
        Guid? organizationId = contextCase == "missing" ? null : OrganizationId;
        PolicyExecution execution = await ExecutePolicyAsync(
            mode,
            organizationGrant: true,
            authorityAllowed: true,
            tenantId,
            organizationId);

        await Assert.That(RequiredBoolean(execution.Decision, "CanCreateManualAddress")).IsFalse();
    }

    [Test]
    public async Task ResolverDoesNotConsultUserScopeToLoosenAParentDenial()
    {
        PolicyExecution execution = await ExecutePolicyAsync(
            "Disabled",
            organizationGrant: true,
            authorityAllowed: true,
            TenantId,
            OrganizationId,
            requestUserId: ActorId);

        await Assert.That(RequiredBoolean(execution.Decision, "CanCreateManualAddress")).IsFalse();
        await Assert.That(execution.SettingContexts.All(context => context.UserId is null)).IsTrue();
        await Assert.That(execution.AuthorizationRequests).IsEmpty();
    }

    [Test]
    [Arguments("AdminOnly", null, true, "TenantApproved", null)]
    [Arguments("AdminOnly", null, false, "CreatorPrivate", null)]
    [Arguments("OrganizationGoverned", "organization", false, "OrganizationScoped", "organization")]
    [Arguments("OpenWithModeration", null, false, "CreatorPrivate", null)]
    [Arguments("OpenWithModeration", "organization", false, "OrganizationScoped", "organization")]
    public async Task ManualGovernanceAppliesOnlyTheTypedResolverDecision(
        string mode,
        string? requestOrganizationCase,
        bool approveAllowed,
        string expectedVisibility,
        string? expectedOrganizationCase)
    {
        Guid? requestOrganizationId = requestOrganizationCase is null ? null : OrganizationId;
        PolicyExecution execution = await ExecutePolicyAsync(
            mode, true, true, TenantId, requestOrganizationId, approveAllowed: approveAllowed);
        await Assert.That(RequiredBoolean(execution.Decision, "CanCreateManualAddress")).IsTrue();
        object source = RequiredProperty(execution.Decision, "InitialSource");
        object visibility = RequiredProperty(execution.Decision, "InitialVisibility");
        Guid? organizationId = ReadRequiredProperty(execution.Decision, "AddressOrganizationId") as Guid?;
        await Assert.That(source.ToString()).IsEqualTo("Manual");
        await Assert.That(visibility.ToString()).IsEqualTo(expectedVisibility);
        await Assert.That(organizationId).IsEqualTo(expectedOrganizationCase is null ? null : OrganizationId);

        Location location = NewLocation();
        location.SetManualAddress("Synthetic governed address", "1000");
        InvokePublicGovernanceTransition(location, ActorId, source, visibility, organizationId);
        await Assert.That(location.CreatedBy).IsEqualTo(ActorId);
        await Assert.That(RequiredProperty(location, "AddressSource").ToString()).IsEqualTo("Manual");
        await Assert.That(RequiredProperty(location, "AddressVisibility").ToString()).IsEqualTo(expectedVisibility);
        await Assert.That(ReadRequiredProperty(location, "AddressOrganizationId")).IsEqualTo(organizationId);
    }

    [Test]
    public async Task ManualGovernanceRejectsOrganizationScopeWithoutTrustedParticipation()
    {
        Location location = NewLocation();
        GovernanceSnapshot before = Snapshot(location);
        PolicyExecution execution = await ExecutePolicyAsync(
            "OrganizationGoverned", true, false, TenantId, OrganizationId);

        await Assert.That(RequiredBoolean(execution.Decision, "CanCreateManualAddress")).IsFalse();
        await Assert.That(execution.AuthorizationRequests).HasSingleItem();
        await Assert.That(execution.AuthorizationRequests[0].Action).IsEqualTo("create_custom_address");
        await Assert.That(Snapshot(location)).IsEqualTo(before);
    }

    [Test]
    public async Task PromotionCommandHasNoCallerControlledTenantSourceOrAuthority()
    {
        Type command = RequiredApplicationType(PromotionCommandTypeName);
        await Assert.That(command.GetProperties(BindingFlags.Instance | BindingFlags.Public).Select(property => property.Name))
            .IsEquivalentTo(["LocationId", "ExpectedConcurrencyStamp"]);
        await Assert.That(command.GetProperty("LocationId")?.PropertyType).IsEqualTo(typeof(Guid));
        await Assert.That(command.GetProperty("ExpectedConcurrencyStamp")?.PropertyType).IsEqualTo(typeof(Guid));
        await Assert.That(command.GetProperty("TenantId")).IsNull();
        await Assert.That(command.GetProperty("AddressSource")).IsNull();
        await Assert.That(command.GetProperty("ActorId")).IsNull();
        await Assert.That(command.GetProperty("IsAuthorized")).IsNull();
    }

    [Test]
    public async Task PromotionValidatorRejectsEmptyLocationAndConcurrencyStamp()
    {
        var validator = new PromoteLocationAddressCommandValidator();

        var result = await validator.ValidateAsync(new PromoteLocationAddressCommand(), CancellationToken.None);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Select(error => error.PropertyName))
            .IsEquivalentTo([nameof(PromoteLocationAddressCommand.LocationId), nameof(PromoteLocationAddressCommand.ExpectedConcurrencyStamp)]);
    }

    [Test]
    public async Task PromotionHandlerValidationFailsBeforeRepositoryAndAuthorization()
    {
        Type handlerType = RequiredApplicationType(PromotionHandlerTypeName);
        var locations = Substitute.For<ILocationRepository>();
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.TenantId.Returns(TenantId);
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(ActorId);
        var authorization = Substitute.For<IAuthorizationProvider>();
        object handler = CreateFromKnownDependencies(handlerType, locations, tenantContext, currentUser, authorization);
        object command = CreateContract(RequiredApplicationType(PromotionCommandTypeName), new Dictionary<string, object?>
        {
            ["LocationId"] = Guid.Empty,
            ["ExpectedConcurrencyStamp"] = Guid.Empty
        });

        object response = await InvokeTaskResultAsync(handler, "Handle", command, CancellationToken.None);

        await Assert.That(RequiredBoolean(response, "Success")).IsFalse();
        string[] errors = ((IEnumerable<string>)RequiredProperty(response, "Errors")).ToArray();
        await Assert.That(errors).Count().IsEqualTo(2);
        await Assert.That(locations.ReceivedCalls()).IsEmpty();
        await Assert.That(authorization.ReceivedCalls()).IsEmpty();
    }

    [Test]
    public async Task PromotionHandlerCancellationIsImmediate()
    {
        Type handlerType = RequiredApplicationType(PromotionHandlerTypeName);
        var locations = Substitute.For<ILocationRepository>();
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.TenantId.Returns(TenantId);
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(ActorId);
        var authorization = Substitute.For<IAuthorizationProvider>();
        object handler = CreateFromKnownDependencies(handlerType, locations, tenantContext, currentUser, authorization);
        object command = CreateContract(RequiredApplicationType(PromotionCommandTypeName), new Dictionary<string, object?>
        {
            ["LocationId"] = NewLocation().Id,
            ["ExpectedConcurrencyStamp"] = NewLocation().ConcurrencyStamp
        });
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            InvokeTaskResultAsync(handler, "Handle", command, cancellation.Token));
        await Assert.That(locations.ReceivedCalls()).IsEmpty();
        await Assert.That(authorization.ReceivedCalls()).IsEmpty();
    }

    [Test]
    public async Task PromotionHandlerFailsClosedBeforeLoadWhenTrustedContextIsMissing()
    {
        Type handlerType = RequiredApplicationType(PromotionHandlerTypeName);
        var locations = Substitute.For<ILocationRepository>();
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.TenantId.Returns(Guid.Empty);
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.IsAuthenticated.Returns(false);
        currentUser.UserId.Returns((Guid?)null);
        var authorization = Substitute.For<IAuthorizationProvider>();
        object handler = CreateFromKnownDependencies(handlerType, locations, tenantContext, currentUser, authorization);
        object command = CreateContract(RequiredApplicationType(PromotionCommandTypeName), new Dictionary<string, object?>
        {
            ["LocationId"] = NewLocation().Id,
            ["ExpectedConcurrencyStamp"] = NewLocation().ConcurrencyStamp
        });

        object response = await InvokeTaskResultAsync(handler, "Handle", command, CancellationToken.None);

        await Assert.That(RequiredBoolean(response, "Success")).IsFalse();
        await Assert.That(locations.ReceivedCalls()).IsEmpty();
        await Assert.That(authorization.ReceivedCalls()).IsEmpty();
    }

    [Test]
    public async Task PromotionHandlerMissingRowFailsClosedWithoutAuthorization()
    {
        Type handlerType = RequiredApplicationType(PromotionHandlerTypeName);
        var locations = Substitute.For<ILocationRepository>();
        locations.GetById(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Location?)null);
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.TenantId.Returns(TenantId);
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(ActorId);
        var authorization = Substitute.For<IAuthorizationProvider>();
        object handler = CreateFromKnownDependencies(handlerType, locations, tenantContext, currentUser, authorization);
        object command = CreateContract(RequiredApplicationType(PromotionCommandTypeName), new Dictionary<string, object?>
        {
            ["LocationId"] = NewLocation().Id,
            ["ExpectedConcurrencyStamp"] = NewLocation().ConcurrencyStamp
        });

        object response = await InvokeTaskResultAsync(handler, "Handle", command, CancellationToken.None);

        await Assert.That(RequiredBoolean(response, "Success")).IsFalse();
        await Assert.That(authorization.ReceivedCalls()).IsEmpty();
        await locations.DidNotReceive().Update(Arg.Any<Location>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task PromotionCancellationImmediatelyBeforeAuthorizationReturnsAllowLeavesAggregateUnchanged()
    {
        using var cancellation = new CancellationTokenSource();
        Location location = NewPromotableLocation();
        GovernanceSnapshot before = Snapshot(location);
        var locations = Substitute.For<ILocationRepository>();
        locations.GetById(location.Id, Arg.Any<CancellationToken>()).Returns(location);
        var authorization = Substitute.For<IAuthorizationProvider>();
        authorization.AuthorizeAsync(Arg.Any<AuthorizationRequest>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                cancellation.Cancel();
                return AuthorizationDecision.Allow(AuthorizationProviderMetadata.Local);
            });
        PromoteLocationAddressCommandHandler handler = CreatePromotionHandler(locations, authorization);

        await Assert.That(async () => await handler.Handle(
            PromotionCommand(location),
            cancellation.Token)).Throws<OperationCanceledException>();

        await Assert.That(Snapshot(location)).IsEqualTo(before);
        await locations.DidNotReceive().Update(Arg.Any<Location>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ProviderCancellationWithLiveRequestTokenPropagatesWithoutMutationOrWrite()
    {
        using var providerCancellation = new CancellationTokenSource();
        await providerCancellation.CancelAsync();
        Location location = NewPromotableLocation();
        GovernanceSnapshot before = Snapshot(location);
        var locations = Substitute.For<ILocationRepository>();
        locations.GetById(location.Id, Arg.Any<CancellationToken>()).Returns(location);
        var authorization = Substitute.For<IAuthorizationProvider>();
        authorization.AuthorizeAsync(Arg.Any<AuthorizationRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromCanceled<AuthorizationDecision>(providerCancellation.Token));
        PromoteLocationAddressCommandHandler handler = CreatePromotionHandler(locations, authorization);

        OperationCanceledException? exception = await Assert.ThrowsAsync<OperationCanceledException>(() =>
            handler.Handle(PromotionCommand(location), CancellationToken.None));

        await Assert.That(exception!.CancellationToken).IsEqualTo(providerCancellation.Token);
        await Assert.That(Snapshot(location)).IsEqualTo(before);
        await locations.DidNotReceive().Update(Arg.Any<Location>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task PromotionCancellationAfterLoadBeforeAuthorizationLeavesAggregateUnchanged()
    {
        using var cancellation = new CancellationTokenSource();
        Location location = NewPromotableLocation();
        GovernanceSnapshot before = Snapshot(location);
        var locations = Substitute.For<ILocationRepository>();
        locations.GetById(location.Id, Arg.Any<CancellationToken>()).Returns(_ =>
        {
            cancellation.Cancel();
            return location;
        });
        var authorization = Substitute.For<IAuthorizationProvider>();
        PromoteLocationAddressCommandHandler handler = CreatePromotionHandler(locations, authorization);

        await Assert.That(async () => await handler.Handle(
            PromotionCommand(location),
            cancellation.Token)).Throws<OperationCanceledException>();

        await Assert.That(Snapshot(location)).IsEqualTo(before);
        await authorization.DidNotReceive().AuthorizeAsync(
            Arg.Any<AuthorizationRequest>(),
            Arg.Any<CancellationToken>());
        await locations.DidNotReceive().Update(Arg.Any<Location>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SameTargetCancellationAfterAuthorizationIsObservedWithoutWrite()
    {
        using var cancellation = new CancellationTokenSource();
        Location location = NewPromotableLocation(LocationAddressVisibilityEnum.TenantApproved);
        GovernanceSnapshot before = Snapshot(location);
        var locations = Substitute.For<ILocationRepository>();
        locations.GetById(location.Id, Arg.Any<CancellationToken>()).Returns(location);
        var authorization = Substitute.For<IAuthorizationProvider>();
        authorization.AuthorizeAsync(Arg.Any<AuthorizationRequest>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                cancellation.Cancel();
                return AuthorizationDecision.Allow(AuthorizationProviderMetadata.Local);
            });
        PromoteLocationAddressCommandHandler handler = CreatePromotionHandler(locations, authorization);

        await Assert.That(async () => await handler.Handle(
            PromotionCommand(location),
            cancellation.Token)).Throws<OperationCanceledException>();

        await Assert.That(Snapshot(location)).IsEqualTo(before);
        await locations.DidNotReceive().Update(Arg.Any<Location>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task AlreadyApprovedCurrentAddressRejectsStaleConcurrencyWithoutMutationOrWrite()
    {
        Location location = NewPromotableLocation(LocationAddressVisibilityEnum.TenantApproved);
        GovernanceSnapshot before = Snapshot(location);
        var locations = Substitute.For<ILocationRepository>();
        locations.GetById(location.Id, Arg.Any<CancellationToken>()).Returns(location);
        var authorization = Substitute.For<IAuthorizationProvider>();
        authorization.AuthorizeAsync(Arg.Any<AuthorizationRequest>(), Arg.Any<CancellationToken>())
            .Returns(AuthorizationDecision.Allow(AuthorizationProviderMetadata.Local));
        PromoteLocationAddressCommandHandler handler = CreatePromotionHandler(locations, authorization);

        ConcurrencyConflictException? exception = await Assert.ThrowsAsync<ConcurrencyConflictException>(() =>
            handler.Handle(new PromoteLocationAddressCommand
            {
                LocationId = location.Id,
                ExpectedConcurrencyStamp = Guid.CreateVersion7()
            }, CancellationToken.None));

        await Assert.That(exception!.Code).IsEqualTo(ConcurrencyConflictException.ConcurrentUpdate);
        await Assert.That(exception.EntityId).IsNull();
        await Assert.That(Snapshot(location)).IsEqualTo(before);
        await authorization.Received(1).AuthorizeAsync(
            Arg.Any<AuthorizationRequest>(),
            CancellationToken.None);
        await locations.DidNotReceive().Update(Arg.Any<Location>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ApprovedStaleDerivedKeysRequireCurrentConcurrencyAndPersistence()
    {
        Location location = NewPromotableLocation(LocationAddressVisibilityEnum.TenantApproved);
        SetPrivateProperty(location, nameof(Location.DisplaySortKey), string.Empty);
        SetPrivateProperty(location, nameof(Location.DisplaySortKeyVersion), (short)0);
        SetPrivateProperty(location.Pii!, nameof(LocationPii.AddressSubstringKey), string.Empty);
        SetPrivateProperty(location.Pii!, nameof(LocationPii.AddressSubstringKeyVersion), (short)0);
        Guid originalStamp = location.ConcurrencyStamp;
        var locations = Substitute.For<ILocationRepository>();
        locations.GetById(location.Id, Arg.Any<CancellationToken>()).Returns(location);
        var authorization = Substitute.For<IAuthorizationProvider>();
        authorization.AuthorizeAsync(Arg.Any<AuthorizationRequest>(), Arg.Any<CancellationToken>())
            .Returns(AuthorizationDecision.Allow(AuthorizationProviderMetadata.Local));
        PromoteLocationAddressCommandHandler handler = CreatePromotionHandler(locations, authorization);

        await Assert.That(async () => await handler.Handle(new PromoteLocationAddressCommand
        {
            LocationId = location.Id,
            ExpectedConcurrencyStamp = Guid.CreateVersion7()
        }, CancellationToken.None)).Throws<ConcurrencyConflictException>();
        await Assert.That(location.DisplaySortKeyVersion).IsEqualTo((short)0);
        await Assert.That(location.Pii.AddressSubstringKeyVersion).IsEqualTo((short)0);
        await locations.DidNotReceive().Update(Arg.Any<Location>(), Arg.Any<CancellationToken>());

        BaseCommandResponse<Guid> response = await handler.Handle(new PromoteLocationAddressCommand
        {
            LocationId = location.Id,
            ExpectedConcurrencyStamp = originalStamp
        }, CancellationToken.None);

        await Assert.That(response.Success).IsTrue();
        await Assert.That(location.DisplaySortKeyVersion).IsEqualTo((short)1);
        await Assert.That(location.Pii.AddressSubstringKeyVersion).IsEqualTo((short)1);
        await Assert.That(location.ConcurrencyStamp).IsNotEqualTo(originalStamp);
        await locations.Received(1).Update(location, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task PromotionHandlerChangesOnlyVisibilityAndAuditConcurrencyFields()
    {
        PromotionExecution execution = await ExecutePromotionAsync(PromotionCase.Success);

        await Assert.That(RequiredBoolean(execution.Response, "Success")).IsTrue();
        await Assert.That(RequiredProperty(execution.Location, "AddressVisibility").ToString()).IsEqualTo("TenantApproved");
        await Assert.That(RequiredProperty(execution.Location, "AddressSource").ToString()).IsEqualTo("Manual");
        await Assert.That(execution.Location.Pii?.Address).IsEqualTo(execution.AddressBefore);
        await Assert.That(execution.Location.Pii?.Postcode).IsEqualTo(execution.PostcodeBefore);
        await Assert.That(execution.Location.GetCoordinate()).IsEqualTo(execution.CoordinateBefore);
        await Assert.That(execution.UpdateCalls).IsEqualTo(1);
        await Assert.That(execution.AuthorizationRequests).HasSingleItem();
        AuthorizationRequest authorization = execution.AuthorizationRequests.Single();
        await Assert.That(authorization.Action).IsEqualTo("approve_tenant_address");
        await Assert.That(authorization.ResourceKind).IsEqualTo(ResourceKinds.Location);
        await Assert.That(authorization.ResourceId).IsEqualTo(execution.Location.Id.ToString());
        await Assert.That(authorization.Subject?.UserId).IsEqualTo(ActorId);
        await Assert.That(authorization.Tenant?.TenantId).IsEqualTo(TenantId);
        await Assert.That(execution.Location.ConcurrencyStamp).IsNotEqualTo(execution.Before.ConcurrencyStamp);
        await Assert.That(ReadRequiredProperty(execution.Location, "AddressOrganizationId")).IsEqualTo(OrganizationId);
        GovernanceSnapshot after = Snapshot(execution.Location);
        await Assert.That(after).IsEqualTo(execution.Before with
        {
            Visibility = "TenantApproved",
            ConcurrencyStamp = after.ConcurrencyStamp,
            UpdatedAt = after.UpdatedAt,
            UpdatedBy = after.UpdatedBy
        });
    }

    [Test]
    [Arguments(PromotionCase.SameTarget, true, 0)]
    [Arguments(PromotionCase.StaleStamp, false, 0)]
    [Arguments(PromotionCase.Unauthorized, false, 0)]
    [Arguments(PromotionCase.ForeignTenant, false, 0)]
    [Arguments(PromotionCase.Erased, false, 0)]
    [Arguments(PromotionCase.PrivateHome, false, 0)]
    [Arguments(PromotionCase.MissingPii, false, 0)]
    [Arguments(PromotionCase.ProviderFailure, false, 0)]
    [Arguments(PromotionCase.SameTargetUnauthorized, false, 0)]
    public async Task PromotionHandlerIsIdempotentAndRejectsUnsafeRows(
        PromotionCase promotionCase,
        bool expectedSuccess,
        int expectedUpdates)
    {
        PromotionExecution execution = await ExecutePromotionAsync(promotionCase);

        bool success = execution.Response is Exception
            ? false
            : RequiredBoolean(execution.Response, "Success");
        await Assert.That(success).IsEqualTo(expectedSuccess);
        await Assert.That(execution.UpdateCalls).IsEqualTo(expectedUpdates);
        await Assert.That(Snapshot(execution.Location)).IsEqualTo(execution.Before);
    }

    [Test]
    [Arguments(PromotionCase.ProviderSource, "ProviderSelection")]
    [Arguments(PromotionCase.UnknownLegacySource, "UnknownLegacy")]
    public async Task PromotionPreservesProviderAndUnknownLegacySource(
        PromotionCase promotionCase,
        string expectedSource)
    {
        PromotionExecution execution = await ExecutePromotionAsync(promotionCase);

        await Assert.That(RequiredBoolean(execution.Response, "Success")).IsTrue();
        await Assert.That(RequiredProperty(execution.Location, "AddressSource").ToString()).IsEqualTo(expectedSource);
        await Assert.That(RequiredProperty(execution.Location, "AddressVisibility").ToString()).IsEqualTo("TenantApproved");
        await Assert.That(execution.UpdateCalls).IsEqualTo(1);
    }

    [Test]
    public async Task PromotionHandlerTwoSynchronizedContendersHaveOneWinnerAndOneStaleResult()
    {
        Type commandType = RequiredApplicationType(PromotionCommandTypeName);
        Type handlerType = RequiredApplicationType(PromotionHandlerTypeName);
        Type sourceType = RequiredDomainType(SourceTypeName);
        Type visibilityType = RequiredDomainType(VisibilityTypeName);
        var locations = Substitute.For<ILocationRepository>();
        var bothLoaded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int arrivals = 0;
        int committedUpdates = 0;
        int staleResults = 0;
        locations.GetById(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(async _ =>
        {
            Location contender = NewLocation();
            contender.SetManualAddress("Synthetic promotion address", "1000");
            InvokePublicGovernanceTransition(contender, ActorId, Enum.Parse(sourceType, "Manual"),
                Enum.Parse(visibilityType, "OrganizationScoped"), OrganizationId);
            if (Interlocked.Increment(ref arrivals) == 2)
            {
                bothLoaded.TrySetResult();
            }
            await bothLoaded.Task.WaitAsync(TimeSpan.FromSeconds(5));
            return (Location?)contender;
        });
        locations.Update(Arg.Any<Location>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            if (Interlocked.CompareExchange(ref committedUpdates, 1, 0) == 0)
            {
                return Task.CompletedTask;
            }
            Interlocked.Increment(ref staleResults);
            throw new ConcurrencyConflictException(
                ConcurrencyConflictException.ConcurrentUpdate,
                "Concurrent location promotion rejected.",
                nameof(Location),
                NewLocation().Id.ToString());
        });
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.TenantId.Returns(TenantId);
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(ActorId);
        var authorization = Substitute.For<IAuthorizationProvider>();
        authorization.AuthorizeAsync(Arg.Any<AuthorizationRequest>(), Arg.Any<CancellationToken>())
            .Returns(AuthorizationDecision.Allow(AuthorizationProviderMetadata.Local));
        object firstHandler = CreateFromKnownDependencies(handlerType, locations, tenantContext, currentUser, authorization);
        object secondHandler = CreateFromKnownDependencies(handlerType, locations, tenantContext, currentUser, authorization);
        object command = CreateContract(commandType, new Dictionary<string, object?>
        {
            ["LocationId"] = NewLocation().Id,
            ["ExpectedConcurrencyStamp"] = NewLocation().ConcurrencyStamp
        });

        object[] outcomes = await Task.WhenAll(
            InvokePromotionOutcomeAsync(firstHandler, command),
            InvokePromotionOutcomeAsync(secondHandler, command));

        await Assert.That(committedUpdates).IsEqualTo(1);
        await Assert.That(staleResults).IsEqualTo(1);
        await Assert.That(outcomes.Count(outcome => outcome is ConcurrencyConflictException)).IsEqualTo(1);
        await Assert.That(outcomes.Count(outcome => outcome is not Exception && RequiredBoolean(outcome, "Success"))).IsEqualTo(1);
    }

    [Test]
    public async Task PrivateHomeErasureRemainsAnIrreversibleSafeControl()
    {
        Location location = NewLocation();
        location.ClassifyAsPrivateHome(ActorId);
        location.SetManualAddress("Synthetic safe-control address", "0000");
        location.EraseOwnedPii(DateTime.UnixEpoch.AddDays(1), LocationPrivacyErasureReasonEnum.OwnerErasureRequest);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Task.Run(() => location.SetManualAddress("Synthetic replacement", "0001")));
        await Assert.That(location.Pii).IsNull();
    }

    private static async Task<PolicyExecution> ExecutePolicyAsync(
        string? mode,
        bool? organizationGrant,
        bool authorityAllowed,
        Guid? tenantId,
        Guid? organizationId,
        Guid? requestUserId = null,
        bool approveAllowed = false)
    {
        Type resolverType = RequiredApplicationType(ResolverTypeName);
        Type requestType = RequiredApplicationType(RequestTypeName);
        Type definitions = RequiredDomainType("Explore.Domain.Settings.Definitions.AddressGovernanceSettingDefinitions");
        string modeKey = RequiredDefinition(definitions, "CreationMode").Key;
        string grantKey = RequiredDefinition(definitions, "OrganizationCreationGrant").Key;
        var settings = Substitute.For<IHierarchicalSettingsResolver>();
        var authorization = Substitute.For<IAuthorizationProvider>();
        var settingContexts = new List<SettingContext>();
        var authorizationRequests = new List<AuthorizationRequest>();

        settings.ResolveAsync<string>(modeKey, Arg.Do<SettingContext>(settingContexts.Add), Arg.Any<CancellationToken>())
            .Returns(mode);
        settings.ResolveAsync<bool>(grantKey, Arg.Do<SettingContext>(settingContexts.Add), Arg.Any<CancellationToken>())
            .Returns(organizationGrant ?? false);
        authorization.AuthorizeAsync(Arg.Do<AuthorizationRequest>(authorizationRequests.Add), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                AuthorizationRequest request = call.Arg<AuthorizationRequest>()
                    ?? throw new InvalidOperationException("Authorization request must not be null.");
                bool allowed = request.Action == "approve_tenant_address" ? approveAllowed : authorityAllowed;
                return allowed
                    ? AuthorizationDecision.Allow(AuthorizationProviderMetadata.Local)
                    : AuthorizationDecision.Deny(AuthorizationProviderMetadata.Local);
            });

        object resolver = Activator.CreateInstance(resolverType, settings, authorization)
            ?? throw new InvalidOperationException("Address governance resolver must own settings and authorization dependencies.");
        object request = CreateContract(requestType, new Dictionary<string, object?>
        {
            ["TenantId"] = tenantId,
            ["ActorId"] = ActorId,
            ["UserId"] = requestUserId ?? ActorId,
            ["OrganizationId"] = organizationId
        });
        object decision = await InvokeTaskResultAsync(resolver, "ResolveAsync", request, CancellationToken.None);
        string[] settingKeys = settings.ReceivedCalls()
            .Where(call => call.GetMethodInfo().Name == nameof(IHierarchicalSettingsResolver.ResolveAsync))
            .Select(call => call.GetArguments()[0] as string
                ?? throw new InvalidOperationException("Address policy setting call requires an exact key."))
            .ToArray();
        await Assert.That(settingKeys).Contains(modeKey);
        await Assert.That(settingKeys.All(key => key == modeKey || key == grantKey)).IsTrue();
        await Assert.That(settingContexts.All(context =>
            context.TenantId == tenantId && context.OrganizationId == organizationId && context.UserId is null)).IsTrue();
        return new PolicyExecution(decision, settingContexts, authorizationRequests, settingKeys);
    }

    private static async Task<PromotionExecution> ExecutePromotionAsync(PromotionCase promotionCase)
    {
        Type commandType = RequiredApplicationType(PromotionCommandTypeName);
        Type handlerType = RequiredApplicationType(PromotionHandlerTypeName);
        Type sourceType = RequiredDomainType(SourceTypeName);
        Type visibilityType = RequiredDomainType(VisibilityTypeName);
        Location location = NewLocation();
        if (promotionCase != PromotionCase.MissingPii)
        {
            location.SetManualAddress("Synthetic promotion address", "1000");
        }
        string sourceName = promotionCase switch
        {
            PromotionCase.ProviderSource => "ProviderSelection",
            PromotionCase.UnknownLegacySource => "UnknownLegacy",
            _ => "Manual"
        };
        bool sameTarget = promotionCase is PromotionCase.SameTarget or PromotionCase.SameTargetUnauthorized;
        string visibilityName = promotionCase == PromotionCase.UnknownLegacySource
            ? "Quarantined"
            : sameTarget ? "TenantApproved" : "OrganizationScoped";
        Guid? organizationId = visibilityName == "OrganizationScoped" ? OrganizationId : null;
        InvokePublicGovernanceTransition(location, ActorId, Enum.Parse(sourceType, sourceName),
            Enum.Parse(visibilityType, visibilityName), organizationId);
        if (promotionCase == PromotionCase.PrivateHome)
        {
            InvokePublicGovernanceTransition(location, ActorId, Enum.Parse(sourceType, "Manual"),
                Enum.Parse(visibilityType, "CreatorPrivate"), null);
            location.ClassifyAsPrivateHome(ActorId);
        }
        if (promotionCase == PromotionCase.Erased)
        {
            InvokePublicGovernanceTransition(location, ActorId, Enum.Parse(sourceType, "Manual"),
                Enum.Parse(visibilityType, "CreatorPrivate"), null);
            location.ClassifyAsPrivateHome(ActorId);
            location.EraseOwnedPii(DateTime.UnixEpoch.AddDays(2), LocationPrivacyErasureReasonEnum.OwnerErasureRequest);
        }
        if (promotionCase == PromotionCase.ForeignTenant)
        {
            location.TenantId = Guid.Parse("019b0000-0001-7000-8000-000000000099");
        }

        var locations = Substitute.For<ILocationRepository>();
        locations.GetById(location.Id, Arg.Any<CancellationToken>()).Returns(location);
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.TenantId.Returns(TenantId);
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(ActorId);
        var authorization = Substitute.For<IAuthorizationProvider>();
        var requests = new List<AuthorizationRequest>();
        authorization.AuthorizeAsync(Arg.Do<AuthorizationRequest>(requests.Add), Arg.Any<CancellationToken>())
            .Returns(_ => promotionCase switch
            {
                PromotionCase.Unauthorized or PromotionCase.SameTargetUnauthorized =>
                    AuthorizationDecision.Deny(AuthorizationProviderMetadata.Local),
                PromotionCase.ProviderFailure => throw new InvalidOperationException("Synthetic authorization provider failure."),
                _ => AuthorizationDecision.Allow(AuthorizationProviderMetadata.Local)
            });

        object handler = CreateFromKnownDependencies(handlerType, locations, tenantContext, currentUser, authorization);
        Guid expectedStamp = promotionCase == PromotionCase.StaleStamp
            ? Guid.Parse("019b0000-0001-7000-8000-000000000088")
            : location.ConcurrencyStamp;
        object command = CreateContract(commandType, new Dictionary<string, object?>
        {
            ["LocationId"] = location.Id,
            ["ExpectedConcurrencyStamp"] = expectedStamp
        });
        GovernanceSnapshot before = Snapshot(location);
        string? address = location.Pii?.Address;
        string? postcode = location.Pii?.Postcode;
        GeoCoordinate? coordinate = location.GetCoordinate();
        object response;
        try
        {
            response = await InvokeTaskResultAsync(handler, "Handle", command, CancellationToken.None);
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            response = exception.InnerException;
        }
        catch (Exception exception)
        {
            response = exception;
        }

        int updates = locations.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(ILocationRepository.Update));
        return new PromotionExecution(response, location, before, address, postcode, coordinate, updates, requests);
    }

    private static object CreateFromKnownDependencies(
        Type type,
        ILocationRepository locations,
        ITenantContext tenantContext,
        ICurrentUserService currentUser,
        IAuthorizationProvider authorization)
    {
        var dependencies = new Dictionary<Type, object>
        {
            [typeof(ILocationRepository)] = locations,
            [typeof(ITenantContext)] = tenantContext,
            [typeof(ICurrentUserService)] = currentUser,
            [typeof(IAuthorizationProvider)] = authorization,
            [typeof(TimeProvider)] = TimeProvider.System
        };
        ConstructorInfo constructor = type.GetConstructors().Single();
        object?[] arguments = constructor.GetParameters().Select(parameter =>
            dependencies.TryGetValue(parameter.ParameterType, out object? dependency)
                ? dependency
                : throw new InvalidOperationException($"Unsupported public promotion dependency {parameter.ParameterType.FullName}.")).ToArray();
        return constructor.Invoke(arguments);
    }

    private static void InvokePublicGovernanceTransition(
        Location location,
        Guid actorId,
        object source,
        object visibility,
        Guid? organizationId)
    {
        MethodInfo? transition = typeof(Location).GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .SingleOrDefault(method =>
            {
                ParameterInfo[] parameters = method.GetParameters();
                return parameters.Length == 4
                    && parameters[0].ParameterType == typeof(Guid)
                    && parameters[1].ParameterType == source.GetType()
                    && parameters[2].ParameterType == visibility.GetType()
                    && parameters[3].ParameterType == typeof(Guid?);
            });
        if (transition is null)
        {
            throw new InvalidOperationException("Location requires one public actor-bound address-governance transition.");
        }

        transition.Invoke(location, [actorId, source, visibility, organizationId]);
    }

    private static async Task<object> InvokePromotionOutcomeAsync(object handler, object command)
    {
        try
        {
            return await InvokeTaskResultAsync(handler, "Handle", command, CancellationToken.None);
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

    private static async Task<object> InvokeTaskResultAsync(object target, string methodName, object request, CancellationToken token)
    {
        MethodInfo method = target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Single(candidate => candidate.Name == methodName && candidate.GetParameters().Length == 2);
        object invocation = method.Invoke(target, [request, token])
            ?? throw new InvalidOperationException($"{methodName} returned no task.");
        if (invocation is not Task task)
        {
            throw new InvalidOperationException($"{methodName} must return Task<T>.");
        }

        await task;
        return task.GetType().GetProperty("Result")?.GetValue(task)
            ?? throw new InvalidOperationException($"{methodName} returned no decision.");
    }

    private static void SetPrivateProperty(object target, string propertyName, object value) =>
        target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)!.SetValue(target, value);

    private static object CreateContract(Type type, Dictionary<string, object?> values)
    {
        ConstructorInfo? constructor = type.GetConstructors().OrderBy(candidate => candidate.GetParameters().Length)
            .FirstOrDefault(candidate => candidate.GetParameters().All(parameter =>
                parameter.Name is not null && values.ContainsKey(parameter.Name)));
        object instance = constructor is null
            ? Activator.CreateInstance(type) ?? throw new InvalidOperationException($"{type.FullName} is not constructible.")
            : constructor.Invoke(constructor.GetParameters().Select(parameter => values[parameter.Name ?? string.Empty]).ToArray());
        foreach ((string name, object? value) in values)
        {
            PropertyInfo? property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
            if (property?.CanWrite == true)
            {
                property.SetValue(instance, value);
            }
        }
        return instance;
    }

    private static async Task AssertEnumAsync(Type type, IReadOnlyDictionary<string, int> expected)
    {
        await Assert.That(Enum.GetNames(type)).IsEquivalentTo(expected.Keys);
        foreach ((string name, int value) in expected)
        {
            await Assert.That(Convert.ToInt32(Enum.Parse(type, name), System.Globalization.CultureInfo.InvariantCulture))
                .IsEqualTo(value);
        }
    }

    private static GovernanceSnapshot Snapshot(Location location) => new(
        ReadRequiredProperty(location, "AddressSource")?.ToString(),
        ReadRequiredProperty(location, "AddressVisibility")?.ToString(),
        ReadRequiredProperty(location, "AddressOrganizationId") as Guid?,
        location.CreatedBy,
        location.Pii?.Address,
        location.Pii?.Postcode,
        location.Pii?.Latitude,
        location.Pii?.Longitude,
        location.ConcurrencyStamp,
        location.UpdatedAt,
        location.UpdatedBy);

    private static bool RequiredBoolean(object instance, string name)
    {
        object? value = ReadRequiredProperty(instance, name);
        return value is bool result
            ? result
            : throw new InvalidOperationException($"{instance.GetType().FullName}.{name} must be Boolean.");
    }

    private static object RequiredProperty(object instance, string name) =>
        ReadRequiredProperty(instance, name)
        ?? throw new InvalidOperationException($"{instance.GetType().FullName}.{name} must not be null.");

    private static object? ReadRequiredProperty(object instance, string name)
    {
        PropertyInfo property = instance.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public)
            ?? throw new InvalidOperationException($"{instance.GetType().FullName} is missing public {name}.");
        return property.GetValue(instance);
    }

    private static string RequiredConstant(Type type, string name) =>
        type.GetField(name, BindingFlags.Public | BindingFlags.Static)?.GetRawConstantValue() as string
        ?? throw new InvalidOperationException($"{type.FullName} is missing public constant {name}.");

    private static SettingDefinition RequiredDefinition(Type type, string name) =>
        type.GetField(name, BindingFlags.Public | BindingFlags.Static)?.GetValue(null) as SettingDefinition
        ?? throw new InvalidOperationException($"{type.FullName} is missing public definition {name}.");

    private static Type RequiredDomainType(string name) => typeof(Location).Assembly.GetType(name, throwOnError: false)
        ?? throw new InvalidOperationException($"Domain contract {name} is missing.");

    private static Type RequiredApplicationType(string name) => typeof(AuthorizationActions).Assembly.GetType(name, throwOnError: false)
        ?? throw new InvalidOperationException($"Application contract {name} is missing.");

    private static PromoteLocationAddressCommandHandler CreatePromotionHandler(
        ILocationRepository locations,
        IAuthorizationProvider authorization)
    {
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.TenantId.Returns(TenantId);
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(ActorId);
        return new PromoteLocationAddressCommandHandler(
            locations,
            tenantContext,
            currentUser,
            authorization,
            TimeProvider.System);
    }

    private static PromoteLocationAddressCommand PromotionCommand(Location location) => new()
    {
        LocationId = location.Id,
        ExpectedConcurrencyStamp = location.ConcurrencyStamp
    };

    private static Location NewPromotableLocation(
        LocationAddressVisibilityEnum visibility = LocationAddressVisibilityEnum.OrganizationScoped)
    {
        Location location = NewLocation();
        location.SetManualAddress("Synthetic promotion address", "1000");
        location.ApplyAddressGovernance(
            ActorId,
            LocationAddressSourceEnum.Manual,
            visibility,
            visibility == LocationAddressVisibilityEnum.OrganizationScoped ? OrganizationId : null);
        return location;
    }

    private static Location NewLocation() => new()
    {
        Id = Guid.Parse("019b0000-0001-7000-8000-000000000010"),
        TenantId = TenantId,
        FullName = "Synthetic governance location",
        Country = "BE",
        City = "Brussels",
        CreatedAt = DateTime.UnixEpoch,
        ConcurrencyStamp = Guid.Parse("019b0000-0001-7000-8000-000000000011")
    };

    public enum PromotionCase
    {
        Success,
        SameTarget,
        StaleStamp,
        Unauthorized,
        ForeignTenant,
        Erased,
        PrivateHome,
        MissingPii,
        ProviderFailure,
        SameTargetUnauthorized,
        ProviderSource,
        UnknownLegacySource
    }

    private sealed record PolicyExecution(
        object Decision,
        List<SettingContext> SettingContexts,
        List<AuthorizationRequest> AuthorizationRequests,
        string[] SettingKeys);

    private sealed record PromotionExecution(
        object Response,
        Location Location,
        GovernanceSnapshot Before,
        string? AddressBefore,
        string? PostcodeBefore,
        GeoCoordinate? CoordinateBefore,
        int UpdateCalls,
        List<AuthorizationRequest> AuthorizationRequests);

    private sealed record GovernanceSnapshot(
        string? Source,
        string? Visibility,
        Guid? OrganizationId,
        Guid? CreatedBy,
        string? Address,
        string? Postcode,
        double? Latitude,
        double? Longitude,
        Guid ConcurrencyStamp,
        DateTime? UpdatedAt,
        Guid? UpdatedBy);
}
