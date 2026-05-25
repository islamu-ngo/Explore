// ABOUTME: Unit tests for building provider-neutral manifests from bundled Cerbos policy artifacts.
// ABOUTME: Verifies namespaced package validation and stable manifest hashing before Admin API upload is added.

using Explore.Application.Authorization;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Models;
using Explore.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Explore.Infrastructure.Tests.Infrastructure;

public class CerbosPolicyPackageServiceTests : IDisposable
{
    private readonly string _tempRoot = Path.Combine(Path.GetTempPath(), $"cerbos-package-{Guid.NewGuid():N}");

    [Test]
    public async Task BuildManifestAsync_WithNamespacedArtifacts_ReturnsStableManifest()
    {
        var policiesRoot = CreatePackageRoot();
        await File.WriteAllTextAsync(Path.Combine(policiesRoot, "islamuevent_event.yaml"), "resourcePolicy:\n  resource: islamuevent_event\n");
        await File.WriteAllTextAsync(Path.Combine(policiesRoot, "derived_roles.yaml"), "derivedRoles:\n  name: islamuevent_explore_admin_roles\n");
        await File.WriteAllTextAsync(Path.Combine(policiesRoot, "_schemas", "islamuevent_event.json"), "{\"type\":\"object\"}");
        await File.WriteAllTextAsync(Path.Combine(policiesRoot, "_schemas", "islamuevent_principal.json"), "{\"type\":\"object\"}");

        var service = CreateService(policiesRoot);

        var manifest = await service.BuildManifestAsync();

        await Assert.That(manifest.PackageId).IsEqualTo("test-policy-package");
        await Assert.That(manifest.Version).IsEqualTo(manifest.ContentHash);
        await Assert.That(manifest.ContentHash.Length).IsEqualTo(64);
        await Assert.That(manifest.Artifacts.Count).IsEqualTo(4);
        await Assert.That(manifest.Artifacts.Select(a => a.LogicalId)).IsEquivalentTo([
            "_schemas/islamuevent_event.json",
            "_schemas/islamuevent_principal.json",
            "derived_roles.yaml",
            "islamuevent_event.yaml"
        ]);
        await Assert.That(manifest.Artifacts.Single(a => a.LogicalId == "islamuevent_event.yaml").Kind)
            .IsEqualTo(PolicyArtifactKind.Policy);
        await Assert.That(manifest.Artifacts.Single(a => a.LogicalId == "_schemas/islamuevent_event.json").Kind)
            .IsEqualTo(PolicyArtifactKind.Schema);
    }

    [Test]
    public async Task BuildManifestAsync_WithLegacyPolicyFile_Throws()
    {
        var policiesRoot = CreatePackageRoot();
        await File.WriteAllTextAsync(Path.Combine(policiesRoot, "event.yaml"), "resourcePolicy:\n  resource: event\n");
        await File.WriteAllTextAsync(Path.Combine(policiesRoot, "_schemas", "islamuevent_event.json"), "{\"type\":\"object\"}");

        var service = CreateService(policiesRoot);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await service.BuildManifestAsync());

        await Assert.That(exception).IsNotNull();
        await Assert.That(exception?.Message ?? string.Empty).Contains("must use the 'islamuevent_' namespace prefix");
    }

    [Test]
    public async Task BuildManifestAsync_WithLegacySchemaFile_Throws()
    {
        var policiesRoot = CreatePackageRoot();
        await File.WriteAllTextAsync(Path.Combine(policiesRoot, "islamuevent_event.yaml"), "resourcePolicy:\n  resource: islamuevent_event\n");
        await File.WriteAllTextAsync(Path.Combine(policiesRoot, "_schemas", "event.json"), "{\"type\":\"object\"}");

        var service = CreateService(policiesRoot);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await service.BuildManifestAsync());

        await Assert.That(exception).IsNotNull();
        await Assert.That(exception?.Message ?? string.Empty).Contains("must use the 'islamuevent_' namespace prefix");
    }

    [Test]
    public async Task ExportArchiveAsync_WithNamespacedArtifacts_IncludesPoliciesSchemasManifestAndInstructions()
    {
        var policiesRoot = CreatePackageRoot();
        const string policy = "resourcePolicy:\n  resource: islamuevent_event\n";
        await File.WriteAllTextAsync(Path.Combine(policiesRoot, "islamuevent_event.yaml"), policy);
        await File.WriteAllTextAsync(Path.Combine(policiesRoot, "derived_roles.yaml"), "derivedRoles:\n  name: islamuevent_explore_admin_roles\n");
        await File.WriteAllTextAsync(Path.Combine(policiesRoot, "_schemas", "islamuevent_event.json"), "{\"type\":\"object\"}");
        await File.WriteAllTextAsync(Path.Combine(policiesRoot, "_schemas", "islamuevent_principal.json"), "{\"type\":\"object\"}");

        var handler = new RecordingMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var service = CreateService(policiesRoot, handler: handler);

        var archiveModel = await service.ExportArchiveAsync();

        await Assert.That(archiveModel.ContentType).IsEqualTo("application/zip");
        await Assert.That(archiveModel.FileName).StartsWith("test-policy-package-");
        await Assert.That(archiveModel.FileName).EndsWith(".zip");
        await Assert.That(archiveModel.Manifest.Artifacts.Count).IsEqualTo(4);
        await Assert.That(handler.Requests).IsEmpty();

        using var memoryStream = new MemoryStream(archiveModel.Content);
        using var zipArchive = new ZipArchive(memoryStream, ZipArchiveMode.Read);
        var entryNames = zipArchive.Entries.Select(entry => entry.FullName).ToArray();

        await Assert.That(entryNames).Contains("_schemas/islamuevent_event.json");
        await Assert.That(entryNames).Contains("_schemas/islamuevent_principal.json");
        await Assert.That(entryNames).Contains("derived_roles.yaml");
        await Assert.That(entryNames).Contains("islamuevent_event.yaml");
        await Assert.That(entryNames).Contains("manifest.json");
        await Assert.That(entryNames).Contains("INSTRUCTIONS.md");

        var instructions = await ReadEntryAsync(zipArchive, "INSTRUCTIONS.md");
        await Assert.That(await ReadEntryAsync(zipArchive, "islamuevent_event.yaml")).IsEqualTo(policy);
        await Assert.That(instructions).Contains("docker compose --profile authz run --rm cerbos-policy-sync");
        await Assert.That(instructions).Contains("cerbosctl put policy --recursive .");
        await Assert.That(instructions).Contains("cerbosctl put schema --recursive _schemas");
        await Assert.That(await ReadEntryAsync(zipArchive, "manifest.json")).Contains("test-policy-package");
    }

    [Test]
    public async Task ExportArchiveAsync_WhenPolicyRootMissing_ThrowsSafeUnavailableException()
    {
        var missingRoot = Path.Combine(_tempRoot, "does-not-exist", "policies");
        var service = CreateService(missingRoot);

        var exception = await Assert.ThrowsAsync<PolicyPackageUnavailableException>(async () =>
            await service.ExportArchiveAsync());

        await Assert.That(exception).IsNotNull();
        await Assert.That(exception?.Message ?? string.Empty).Contains("Cerbos:PolicyPackagePath");
        await Assert.That(exception?.Message ?? string.Empty).DoesNotContain(_tempRoot);
        await Assert.That(exception?.Message ?? string.Empty).DoesNotContain("does-not-exist");
    }

    [Test]
    public async Task BuildManifestAsync_WhenSchemaDirectoryMissing_ThrowsSafeUnavailableException()
    {
        var policiesRoot = Path.Combine(_tempRoot, "policies-without-schemas");
        Directory.CreateDirectory(policiesRoot);
        await File.WriteAllTextAsync(Path.Combine(policiesRoot, "islamuevent_event.yaml"), CreatePolicyYaml("islamuevent_event"));
        var service = CreateService(policiesRoot);

        var exception = await Assert.ThrowsAsync<PolicyPackageUnavailableException>(async () =>
            await service.BuildManifestAsync());

        await Assert.That(exception).IsNotNull();
        await Assert.That(exception?.Message ?? string.Empty).Contains("Cerbos:PolicyPackagePath");
        await Assert.That(exception?.Message ?? string.Empty).DoesNotContain(policiesRoot);
    }

    [Test]
    public async Task PublishAsync_WithNamespacedArtifacts_UploadsSchemasBeforePoliciesThenReloads()
    {
        var policiesRoot = CreatePackageRoot();
        const string schema = "{\"type\":\"object\"}";
        await File.WriteAllTextAsync(Path.Combine(policiesRoot, "islamuevent_event.yaml"), "apiVersion: api.cerbos.dev/v1\nresourcePolicy:\n  resource: islamuevent_event\n  version: default\n  rules: []\n");
        await File.WriteAllTextAsync(Path.Combine(policiesRoot, "_schemas", "islamuevent_event.json"), schema);

        var handler = new RecordingMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}")
        });
        var service = CreateService(policiesRoot, handler: handler);

        var result = await service.PublishAsync();

        await Assert.That(result.Succeeded).IsTrue();
        await Assert.That(handler.Requests.Count).IsEqualTo(3);
        await Assert.That(handler.Requests[0].RequestUri?.AbsolutePath).IsEqualTo("/admin/schema");
        await Assert.That(handler.Requests[1].RequestUri?.AbsolutePath).IsEqualTo("/admin/policy");
        await Assert.That(handler.Requests[2].RequestUri?.PathAndQuery).IsEqualTo("/admin/store/reload?wait=true");
        await Assert.That(handler.Requests.All(r => r.Authorization?.Scheme == "Basic")).IsTrue();

        using var schemaPayload = JsonDocument.Parse(handler.Requests[0].Body);
        var schemaItem = schemaPayload.RootElement.GetProperty("schemas")[0];
        await Assert.That(schemaItem.GetProperty("id").GetString()).IsEqualTo("islamuevent_event.json");
        await Assert.That(Encoding.UTF8.GetString(Convert.FromBase64String(schemaItem.GetProperty("definition").GetString() ?? string.Empty)))
            .IsEqualTo(schema);

        using var policyPayload = JsonDocument.Parse(handler.Requests[1].Body);
        var policy = policyPayload.RootElement.GetProperty("policies")[0];
        await Assert.That(policy.GetProperty("apiVersion").GetString()).IsEqualTo("api.cerbos.dev/v1");
        await Assert.That(policy.GetProperty("resourcePolicy").GetProperty("resource").GetString()).IsEqualTo("islamuevent_event");
    }

    [Test]
    public async Task PublishAsync_WithByoAdminConfiguration_UsesResolvedEndpointAndCredentialsOnly()
    {
        var policiesRoot = CreatePackageRoot();
        await File.WriteAllTextAsync(Path.Combine(policiesRoot, "islamuevent_event.yaml"), CreatePolicyYaml("islamuevent_event"));
        await File.WriteAllTextAsync(Path.Combine(policiesRoot, "_schemas", "islamuevent_event.json"), "{\"type\":\"object\"}");

        var handler = new RecordingMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var service = CreateService(
            policiesRoot,
            handler: handler,
            resolvedConfiguration: new CerbosConfiguration
            {
                Endpoint = "tenant-grpc.example:3593",
                Mode = CerbosMode.CustomEndpoint,
                FailureMode = CerbosFailureMode.Closed,
                AdminEndpoint = "https://tenant-cerbos.example/base",
                AdminUsername = "tenant-user",
                AdminPassword = "tenant-secret",
                IsInstanceDefault = false
            });

        var result = await service.PublishAsync();

        await Assert.That(result.Succeeded).IsTrue();
        await Assert.That(handler.Requests.All(r => r.RequestUri?.Host == "tenant-cerbos.example")).IsTrue();
        await Assert.That(handler.Requests.All(r => r.RequestUri?.AbsolutePath.StartsWith("/base/admin", StringComparison.Ordinal) == true)).IsTrue();

        var authorization = handler.Requests[0].Authorization;
        await Assert.That(authorization?.Scheme).IsEqualTo("Basic");
        var decodedCredentials = Encoding.UTF8.GetString(Convert.FromBase64String(authorization?.Parameter ?? string.Empty));
        await Assert.That(decodedCredentials).IsEqualTo("tenant-user:tenant-secret");
    }

    [Test]
    public async Task PublishAsync_WithByoAdminEndpointMissingCredentials_FailsWithoutHttpRequest()
    {
        var policiesRoot = CreatePackageRoot();
        await File.WriteAllTextAsync(Path.Combine(policiesRoot, "islamuevent_event.yaml"), CreatePolicyYaml("islamuevent_event"));
        await File.WriteAllTextAsync(Path.Combine(policiesRoot, "_schemas", "islamuevent_event.json"), "{\"type\":\"object\"}");

        var handler = new RecordingMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var service = CreateService(
            policiesRoot,
            handler: handler,
            resolvedConfiguration: new CerbosConfiguration
            {
                Endpoint = "tenant-grpc.example:3593",
                Mode = CerbosMode.CustomEndpoint,
                FailureMode = CerbosFailureMode.Closed,
                AdminEndpoint = "https://tenant-cerbos.example",
                AdminUsername = "tenant-user",
                AdminPassword = null,
                IsInstanceDefault = false
            });

        var result = await service.PublishAsync();

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.IssueCode).IsEqualTo(PolicyPackageIssueCode.AdminApiNotConfigured);
        await Assert.That(handler.Requests).IsEmpty();
        await Assert.That(string.Join(' ', result.Warnings)).Contains("requires both admin username and admin password");
        await Assert.That(string.Join(' ', result.Warnings)).DoesNotContain("tenant-user");
    }

    [Arguments("http://tenant-cerbos.example")]
    [Arguments("https://localhost:3592")]
    [Arguments("https://127.0.0.1:3592")]
    [Arguments("https://user:password@tenant-cerbos.example")]
    [Test]
    public async Task PublishAsync_WithUnsafeByoAdminEndpoint_FailsWithoutHttpRequest(string adminEndpoint)
    {
        var policiesRoot = CreatePackageRoot();
        await File.WriteAllTextAsync(Path.Combine(policiesRoot, "islamuevent_event.yaml"), CreatePolicyYaml("islamuevent_event"));
        await File.WriteAllTextAsync(Path.Combine(policiesRoot, "_schemas", "islamuevent_event.json"), "{\"type\":\"object\"}");

        var handler = new RecordingMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var service = CreateService(
            policiesRoot,
            handler: handler,
            resolvedConfiguration: new CerbosConfiguration
            {
                Endpoint = "tenant-grpc.example:3593",
                Mode = CerbosMode.CustomEndpoint,
                FailureMode = CerbosFailureMode.Closed,
                AdminEndpoint = adminEndpoint,
                AdminUsername = "tenant-user",
                AdminPassword = "tenant-secret",
                IsInstanceDefault = false
            });

        var result = await service.PublishAsync();

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(handler.Requests).IsEmpty();
        await Assert.That(string.Join(' ', result.Warnings)).DoesNotContain("tenant-secret");
        await Assert.That(string.Join(' ', result.Warnings)).DoesNotContain("password@tenant-cerbos.example");
    }

    [Test]
    public async Task PublishAsync_WithPolicyLimit_SplitsPolicyRequests()
    {
        var policiesRoot = CreatePackageRoot();
        await File.WriteAllTextAsync(Path.Combine(policiesRoot, "_schemas", "islamuevent_event.json"), "{\"type\":\"object\"}");
        await File.WriteAllTextAsync(Path.Combine(policiesRoot, "islamuevent_event.yaml"), CreatePolicyYaml("islamuevent_event"));
        await File.WriteAllTextAsync(Path.Combine(policiesRoot, "islamuevent_tenant.yaml"), CreatePolicyYaml("islamuevent_tenant"));
        await File.WriteAllTextAsync(Path.Combine(policiesRoot, "islamuevent_organization.yaml"), CreatePolicyYaml("islamuevent_organization"));

        var handler = new RecordingMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var service = CreateService(policiesRoot, maxPoliciesPerRequest: 2, handler: handler);

        var result = await service.PublishAsync();

        await Assert.That(result.Succeeded).IsTrue();
        var policyRequests = handler.Requests.Where(r => r.RequestUri?.AbsolutePath == "/admin/policy").ToArray();
        await Assert.That(policyRequests.Length).IsEqualTo(2);
        using var firstBatch = JsonDocument.Parse(policyRequests[0].Body);
        using var secondBatch = JsonDocument.Parse(policyRequests[1].Body);
        await Assert.That(firstBatch.RootElement.GetProperty("policies").GetArrayLength()).IsEqualTo(2);
        await Assert.That(secondBatch.RootElement.GetProperty("policies").GetArrayLength()).IsEqualTo(1);
    }

    [Test]
    public async Task PublishAsync_WhenSchemaUploadFails_ReturnsFailureWithoutPolicyUpload()
    {
        var policiesRoot = CreatePackageRoot();
        await File.WriteAllTextAsync(Path.Combine(policiesRoot, "islamuevent_event.yaml"), CreatePolicyYaml("islamuevent_event"));
        await File.WriteAllTextAsync(Path.Combine(policiesRoot, "_schemas", "islamuevent_event.json"), "{\"type\":\"object\"}");

        var handler = new RecordingMessageHandler(request => request.RequestUri?.AbsolutePath == "/admin/schema"
            ? new HttpResponseMessage(HttpStatusCode.Unauthorized) { Content = new StringContent("bad credentials") }
            : new HttpResponseMessage(HttpStatusCode.OK));
        var service = CreateService(policiesRoot, handler: handler);

        var result = await service.PublishAsync();

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.IssueCode).IsEqualTo(PolicyPackageIssueCode.AdminApiAuthenticationFailed);
        await Assert.That(result.Message).Contains("publish failed");
        await Assert.That(result.Message).Contains("authentication failed");
        await Assert.That(string.Join(' ', result.Warnings)).DoesNotContain("bad credentials");
        await Assert.That(string.Join(' ', result.Warnings)).DoesNotContain("secret");
        await Assert.That(handler.Requests.Select(r => r.RequestUri?.AbsolutePath ?? string.Empty)).IsEquivalentTo(["/admin/schema"]);
    }

    [Test]
    public async Task PublishAsync_WhenSchemaUploadIsUnavailable_ReturnsAdminApiUnavailableIssue()
    {
        var policiesRoot = CreatePackageRoot();
        await File.WriteAllTextAsync(Path.Combine(policiesRoot, "islamuevent_event.yaml"), CreatePolicyYaml("islamuevent_event"));
        await File.WriteAllTextAsync(Path.Combine(policiesRoot, "_schemas", "islamuevent_event.json"), "{\"type\":\"object\"}");

        var handler = new RecordingMessageHandler(request => request.RequestUri?.AbsolutePath == "/admin/schema"
            ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable) { Content = new StringContent("upstream unavailable with secret") }
            : new HttpResponseMessage(HttpStatusCode.OK));
        var service = CreateService(policiesRoot, handler: handler);

        var result = await service.PublishAsync();

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.IssueCode).IsEqualTo(PolicyPackageIssueCode.AdminApiUnavailable);
        await Assert.That(result.Message).Contains("unavailable");
        await Assert.That(string.Join(' ', result.Warnings)).DoesNotContain("upstream unavailable");
        await Assert.That(string.Join(' ', result.Warnings)).DoesNotContain("secret");
        await Assert.That(handler.Requests.Select(r => r.RequestUri?.AbsolutePath ?? string.Empty)).IsEquivalentTo(["/admin/schema"]);
    }

    [Test]
    public async Task PublishAsync_WhenSchemaUploadTransportFails_ReturnsAdminApiUnavailableIssue()
    {
        var policiesRoot = CreatePackageRoot();
        await File.WriteAllTextAsync(Path.Combine(policiesRoot, "islamuevent_event.yaml"), CreatePolicyYaml("islamuevent_event"));
        await File.WriteAllTextAsync(Path.Combine(policiesRoot, "_schemas", "islamuevent_event.json"), "{\"type\":\"object\"}");

        var handler = new RecordingMessageHandler(request => request.RequestUri?.AbsolutePath == "/admin/schema"
            ? throw new HttpRequestException("tenant-secret transport failure")
            : new HttpResponseMessage(HttpStatusCode.OK));
        var service = CreateService(policiesRoot, handler: handler);

        var result = await service.PublishAsync();

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.IssueCode).IsEqualTo(PolicyPackageIssueCode.AdminApiUnavailable);
        await Assert.That(result.Message).Contains("unavailable");
        await Assert.That(string.Join(' ', result.Warnings)).DoesNotContain("tenant-secret");
        await Assert.That(string.Join(' ', result.Warnings)).Contains("failed before a response was received");
        await Assert.That(handler.Requests.Select(r => r.RequestUri?.AbsolutePath ?? string.Empty)).IsEquivalentTo(["/admin/schema"]);
    }

    [Test]
    public async Task PublishAsync_WhenReloadFails_ReturnsFailureAfterUpload()
    {
        var policiesRoot = CreatePackageRoot();
        await File.WriteAllTextAsync(Path.Combine(policiesRoot, "islamuevent_event.yaml"), CreatePolicyYaml("islamuevent_event"));
        await File.WriteAllTextAsync(Path.Combine(policiesRoot, "_schemas", "islamuevent_event.json"), "{\"type\":\"object\"}");

        var handler = new RecordingMessageHandler(request => request.RequestUri?.AbsolutePath == "/admin/store/reload"
            ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            : new HttpResponseMessage(HttpStatusCode.OK));
        var service = CreateService(policiesRoot, handler: handler);

        var result = await service.PublishAsync();

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.IssueCode).IsEqualTo(PolicyPackageIssueCode.ReloadFailed);
        await Assert.That(result.Message).Contains("failed to reload");
        await Assert.That(handler.Requests.Select(r => r.RequestUri?.AbsolutePath ?? string.Empty)).IsEquivalentTo([
            "/admin/schema",
            "/admin/policy",
            "/admin/store/reload"
        ]);
    }

    [Test]
    public async Task PublishAsync_WhenReloadTransportFails_ReturnsReloadFailedIssue()
    {
        var policiesRoot = CreatePackageRoot();
        await File.WriteAllTextAsync(Path.Combine(policiesRoot, "islamuevent_event.yaml"), CreatePolicyYaml("islamuevent_event"));
        await File.WriteAllTextAsync(Path.Combine(policiesRoot, "_schemas", "islamuevent_event.json"), "{\"type\":\"object\"}");

        var handler = new RecordingMessageHandler(request => request.RequestUri?.AbsolutePath == "/admin/store/reload"
            ? throw new HttpRequestException("tenant-secret reload failure")
            : new HttpResponseMessage(HttpStatusCode.OK));
        var service = CreateService(policiesRoot, handler: handler);

        var result = await service.PublishAsync();

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.IssueCode).IsEqualTo(PolicyPackageIssueCode.ReloadFailed);
        await Assert.That(result.Message).Contains("failed to reload");
        await Assert.That(string.Join(' ', result.Warnings)).DoesNotContain("tenant-secret");
        await Assert.That(handler.Requests.Select(r => r.RequestUri?.AbsolutePath ?? string.Empty)).IsEquivalentTo([
            "/admin/schema",
            "/admin/policy",
            "/admin/store/reload"
        ]);
    }

    [Test]
    public async Task GetStatusAsync_WithConfiguredAdminApi_ReturnsUnknownPackageStatusWithoutHttpRequest()
    {
        var policiesRoot = CreatePackageRoot();
        await File.WriteAllTextAsync(Path.Combine(policiesRoot, "islamuevent_event.yaml"), CreatePolicyYaml("islamuevent_event"));
        await File.WriteAllTextAsync(Path.Combine(policiesRoot, "_schemas", "islamuevent_event.json"), "{\"type\":\"object\"}");

        var handler = new RecordingMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var service = CreateService(policiesRoot, handler: handler);

        var result = await service.GetStatusAsync();

        await Assert.That(result.IssueCode).IsEqualTo(PolicyPackageIssueCode.PackageStatusUnknown);
        await Assert.That(result.IsHealthy).IsTrue();
        await Assert.That(result.Message).Contains("remote package hash verification is not available");
        await Assert.That(result.PackageId).IsEqualTo("test-policy-package");
        await Assert.That(result.ContentHash.Length).IsEqualTo(64);
        await Assert.That(handler.Requests).IsEmpty();
    }

    [Test]
    public async Task GetStatusAsync_WithByoAdminEndpointMissingCredentials_ReturnsNotConfiguredWithoutSecretLeak()
    {
        var policiesRoot = CreatePackageRoot();
        await File.WriteAllTextAsync(Path.Combine(policiesRoot, "islamuevent_event.yaml"), CreatePolicyYaml("islamuevent_event"));
        await File.WriteAllTextAsync(Path.Combine(policiesRoot, "_schemas", "islamuevent_event.json"), "{\"type\":\"object\"}");

        var handler = new RecordingMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var service = CreateService(
            policiesRoot,
            handler: handler,
            resolvedConfiguration: new CerbosConfiguration
            {
                Endpoint = "tenant-grpc.example:3593",
                Mode = CerbosMode.CustomEndpoint,
                FailureMode = CerbosFailureMode.Closed,
                AdminEndpoint = "https://tenant-cerbos.example",
                AdminUsername = "tenant-user",
                AdminPassword = null,
                IsInstanceDefault = false
            });

        var result = await service.GetStatusAsync();

        await Assert.That(result.IssueCode).IsEqualTo(PolicyPackageIssueCode.AdminApiNotConfigured);
        await Assert.That(result.IsHealthy).IsFalse();
        await Assert.That(string.Join(' ', result.Warnings)).Contains("requires both admin username and admin password");
        await Assert.That(string.Join(' ', result.Warnings)).DoesNotContain("tenant-user");
        await Assert.That(handler.Requests).IsEmpty();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
            Directory.Delete(_tempRoot, recursive: true);

        GC.SuppressFinalize(this);
    }

    private string CreatePackageRoot()
    {
        var policiesRoot = Path.Combine(_tempRoot, "policies");
        Directory.CreateDirectory(Path.Combine(policiesRoot, "_schemas"));
        return policiesRoot;
    }

    private static CerbosPolicyPackageService CreateService(
        string policiesRoot,
        int maxPoliciesPerRequest = 100,
        RecordingMessageHandler? handler = null,
        CerbosConfiguration? resolvedConfiguration = null)
    {
        var options = Options.Create(new CerbosPolicyPackageOptions
        {
            PoliciesPath = policiesRoot,
            ProductNamespacePrefix = "islamuevent_",
            PackageId = "test-policy-package",
            MaxPoliciesPerRequest = maxPoliciesPerRequest
        });
        var adminOptions = Options.Create(new CerbosAdminApiSettings
        {
            Endpoints = ["https://cerbos.example"],
            AdminUsername = "admin",
            AdminPassword = "secret"
        });

        handler ??= new RecordingMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var configResolver = Substitute.For<ICerbosConfigResolver>();
        configResolver.ResolveAsync(Arg.Any<CancellationToken>()).Returns(resolvedConfiguration ?? new CerbosConfiguration
        {
            Endpoint = "instance-cerbos.example:3593",
            Mode = CerbosMode.Instance,
            FailureMode = CerbosFailureMode.Open,
            IsInstanceDefault = true
        });

        return new CerbosPolicyPackageService(
            options,
            adminOptions,
            configResolver,
            new CerbosAdminEndpointValidator(options),
            new StaticHttpClientFactory(new HttpClient(handler)),
            Substitute.For<ILogger<CerbosPolicyPackageService>>());
    }

    private static string CreatePolicyYaml(string resourceKind)
    {
        return $"apiVersion: api.cerbos.dev/v1\nresourcePolicy:\n  resource: {resourceKind}\n  version: default\n  rules: []\n";
    }

    private static async Task<string> ReadEntryAsync(ZipArchive archive, string entryName)
    {
        var entry = archive.GetEntry(entryName)
            ?? throw new InvalidOperationException($"ZIP entry '{entryName}' was not found.");
        await using var stream = entry.Open();
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return await reader.ReadToEndAsync();
    }

    private sealed class StaticHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;

        public StaticHttpClientFactory(HttpClient client)
        {
            _client = client;
        }

        public HttpClient CreateClient(string name)
        {
            return _client;
        }
    }

    private sealed class RecordingMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

        public RecordingMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        public List<RecordedRequest> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(new RecordedRequest(
                request.Method,
                request.RequestUri,
                request.Headers.Authorization,
                request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken)));

            return _responseFactory(request);
        }
    }

    private sealed record RecordedRequest(
        HttpMethod Method,
        Uri? RequestUri,
        AuthenticationHeaderValue? Authorization,
        string Body);
}
