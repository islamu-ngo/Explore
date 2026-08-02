// ABOUTME: Real TestServer coverage for authorized manual registration-file release and read fencing.
// ABOUTME: Verifies admin HAL affordances, immutable retry audit, and soft-delete quarantine resistance.

using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Event.Api.IntegrationTests.Fixtures;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Models.Storage;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using Explore.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Api.IntegrationTests.Features;

[NotInParallel("ApiTestFixture")]
public sealed class RegistrationAnswerFilesControllerTests
{
    [Test]
    public async Task Release_RequiresAdminAndPersistsOneImmutableAuditBeforeReadsBecomeAvailable()
    {
        Guid adminId = Guid.CreateVersion7();
        var provider = CreateProvider();
        var resolver = Substitute.For<IFileStorageProviderResolver>();
        resolver.GetRequired(StorageProviders.Local).Returns(provider);
        var signing = Substitute.For<IObjectStorageService>();
        signing.GeneratePresignedDownloadUrl(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>())
            .Returns("https://storage.example.test/released");
        await using var factory = CreateFactory(resolver, signing);
        using HttpClient client = factory.CreateClient();
        (Guid fileId, Guid storageId) = await SeedAsync(factory, adminId, softDeleted: false);
        string releasePath = $"/api/registration-answer-files/{fileId}/release";

        using HttpResponseMessage anonymous = await client.PostAsJsonAsync(
            releasePath, new { reason = "Anonymous attempt" });
        using HttpRequestMessage nonAdminRequest = CreateAuthenticatedJsonRequest(
            HttpMethod.Post, releasePath, Guid.CreateVersion7(), "Unprivileged attempt", admin: false);
        using HttpResponseMessage nonAdmin = await client.SendAsync(nonAdminRequest);
        using HttpRequestMessage detailRequest = CreateAuthenticatedRequest(
            HttpMethod.Get, $"/api/registration-answer-files/{fileId}", adminId, admin: true);
        using HttpResponseMessage before = await client.SendAsync(detailRequest);
        string beforeJson = await before.Content.ReadAsStringAsync();

        await Assert.That(anonymous.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
        await Assert.That(nonAdmin.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
        await Assert.That(before.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(beforeJson).Contains("\"release\"");
        await AssertReadSurfacesAsync(client, storageId, adminId, HttpStatusCode.NotFound);
        await provider.DidNotReceive().OpenReadAsync(
            Arg.Any<FileStorageReadInput>(), Arg.Any<CancellationToken>());
        signing.DidNotReceive().GeneratePresignedDownloadUrl(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>());

        using HttpRequestMessage releaseRequest = CreateAuthenticatedJsonRequest(
            HttpMethod.Post, releasePath, adminId, "Verified by tenant administrator", admin: true);
        using HttpResponseMessage released = await client.SendAsync(releaseRequest);
        string releasedJson = await released.Content.ReadAsStringAsync();
        using HttpRequestMessage retryRequest = CreateAuthenticatedJsonRequest(
            HttpMethod.Post, releasePath, adminId, "Retry must not replace audit", admin: true);
        using HttpResponseMessage retry = await client.SendAsync(retryRequest);

        await Assert.That(released.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(retry.StatusCode).IsEqualTo(HttpStatusCode.OK);
        using var releasedDocument = JsonDocument.Parse(releasedJson);
        await Assert.That(releasedDocument.RootElement.GetProperty("_links").TryGetProperty("release", out _)).IsFalse();
        await AssertReadSurfacesAsync(client, storageId, adminId, HttpStatusCode.OK);
        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();
        ExploreDbContext context = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();
        RegistrationAnswerFileRelease audit = await context.RegistrationAnswerFileReleases.SingleAsync();
        RegistrationAnswerFile file = await context.RegistrationAnswerFiles.SingleAsync(item => item.Id == fileId);
        await Assert.That(audit.ReleasedBy).IsEqualTo(adminId);
        await Assert.That(audit.Reason).IsEqualTo("Verified by tenant administrator");
        await Assert.That(file.ReleasedBy).IsEqualTo(adminId);
        await Assert.That(await context.RegistrationAnswerFileReleases.CountAsync()).IsEqualTo(1);
    }

    [Test]
    public async Task SoftDeletedQuarantineAssociation_BlocksAllReadSurfaces()
    {
        Guid adminId = Guid.CreateVersion7();
        var provider = CreateProvider();
        var resolver = Substitute.For<IFileStorageProviderResolver>();
        resolver.GetRequired(StorageProviders.Local).Returns(provider);
        var signing = Substitute.For<IObjectStorageService>();
        signing.GeneratePresignedDownloadUrl(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>())
            .Returns("https://storage.example.test/should-not-be-created");
        await using var factory = CreateFactory(resolver, signing);
        using HttpClient client = factory.CreateClient();
        (_, Guid storageId) = await SeedAsync(factory, adminId, softDeleted: true);

        await AssertReadSurfacesAsync(client, storageId, adminId, HttpStatusCode.NotFound);

        await provider.DidNotReceive().OpenReadAsync(
            Arg.Any<FileStorageReadInput>(), Arg.Any<CancellationToken>());
        signing.DidNotReceive().GeneratePresignedDownloadUrl(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>());
    }

    private static RegistrationFileWebApplicationFactory CreateFactory(
        IFileStorageProviderResolver resolver,
        IObjectStorageService signing)
        => new(resolver, signing)
        {
            AuthorizationProviderOverride = new StubAuthorizationProvider { AllowAll = true }
        };

    private static IFileStorageProvider CreateProvider()
    {
        var provider = Substitute.For<IFileStorageProvider>();
        provider.Provider.Returns(StorageProviders.Local);
        provider.OpenReadAsync(Arg.Any<FileStorageReadInput>(), Arg.Any<CancellationToken>())
            .Returns(_ => new FileStorageReadResult(
                new MemoryStream([1, 2, 3, 4]), "image/png", 4, DateTimeOffset.UtcNow));
        return provider;
    }

    private static async Task<(Guid FileId, Guid StorageId)> SeedAsync(
        RegistrationFileWebApplicationFactory factory,
        Guid createdBy,
        bool softDeleted)
    {
        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();
        ExploreDbContext context = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();
        Guid tenantId = PlatformDefaults.DefaultTenantId;
        RegistrationFormField field = CreateFileField(tenantId);
        var storage = new StorageObject
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            Tenant = null!,
            FileTypeId = (int)FileTypeEnum.Image,
            FileType = null!,
            Uri = "/api/storageobject/registration-file/content",
            ObjectKey = $"tenants/{tenantId:N}/{Guid.NewGuid():N}.png",
            Provider = StorageProviders.Local,
            FullName = "registration-file.png",
            SafeDisplayName = "registration-file.png",
            Extension = "png",
            ContentType = "image/png",
            Sha256Checksum = new string('a', 64),
            Size = 4,
            Visibility = StorageObjectVisibilities.PublicImage,
            Purpose = StorageObjectPurposes.EventImage,
            LifecycleState = StorageObjectLifecycleStates.Active,
            CreatedBy = createdBy,
            ConcurrencyStamp = Guid.CreateVersion7()
        };
        RegistrationAnswerFile file = RegistrationAnswerFile.Create(
            tenantId, Guid.CreateVersion7(), field, storage, DateTime.UtcNow);
        file.IsDeleted = softDeleted;
        context.AddRange(storage, file);
        await context.SaveChangesAsync();
        return (file.Id, storage.Id);
    }

    private static RegistrationFormField CreateFileField(Guid tenantId)
    {
        DateTime now = DateTime.UtcNow;
        RegistrationForm form = RegistrationForm.Create(
            tenantId, Guid.CreateVersion7(), "native", "files", "Files", now);
        RegistrationFormVersion version = RegistrationFormVersion.Create(form, 1, "en", null, null, now);
        RegistrationFormSection section = RegistrationFormSection.Create(
            Guid.CreateVersion7(), version, 1, "Documents", now);
        return RegistrationFormField.Create(
            Guid.CreateVersion7(), section, 1, "native", "document", "Document",
            RegistrationFieldTypeEnum.File, 1,
            RegistrationOrganizerVisibilityEnum.AuthorizedOrganizers,
            false, false, now);
    }

    private static HttpRequestMessage CreateAuthenticatedRequest(
        HttpMethod method,
        string path,
        Guid userId,
        bool admin)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Add(
            TestAuthHandler.AuthHeaderName,
            admin
                ? TestAuthHandler.CreateAuthHeaderValue(userId, "Registration file admin", (ClaimTypes.Role, "Admin"))
                : TestAuthHandler.CreateAuthHeaderValue(userId, "Registration file user"));
        return request;
    }

    private static HttpRequestMessage CreateAuthenticatedJsonRequest(
        HttpMethod method,
        string path,
        Guid userId,
        string reason,
        bool admin)
    {
        HttpRequestMessage request = CreateAuthenticatedRequest(method, path, userId, admin);
        request.Content = JsonContent.Create(new { reason });
        return request;
    }

    private static async Task AssertReadSurfacesAsync(
        HttpClient client,
        Guid storageId,
        Guid userId,
        HttpStatusCode expected)
    {
        using HttpRequestMessage contentRequest = CreateAuthenticatedRequest(
            HttpMethod.Get, $"/api/storageobject/{storageId}/content", userId, admin: true);
        using HttpResponseMessage content = await client.SendAsync(contentRequest);
        using HttpResponseMessage publicImage = await client.GetAsync($"/api/storageobject/{storageId}/public");
        using HttpRequestMessage presignedRequest = CreateAuthenticatedRequest(
            HttpMethod.Get, $"/api/storageobject/{storageId}/presigned-url", userId, admin: true);
        using HttpResponseMessage presigned = await client.SendAsync(presignedRequest);
        await Assert.That(content.StatusCode).IsEqualTo(expected);
        await Assert.That(publicImage.StatusCode).IsEqualTo(expected);
        await Assert.That(presigned.StatusCode).IsEqualTo(expected);
    }

    private sealed class RegistrationFileWebApplicationFactory(
        IFileStorageProviderResolver resolver,
        IObjectStorageService signing) : AuthenticatedWebApplicationFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IFileStorageProviderResolver>();
                services.AddSingleton(resolver);
                services.RemoveAll<IObjectStorageService>();
                services.AddSingleton(signing);
            });
        }
    }
}
