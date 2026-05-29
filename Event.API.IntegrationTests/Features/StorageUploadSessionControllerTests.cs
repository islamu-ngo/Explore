// ABOUTME: API contract tests for provider-neutral storage upload session endpoints.
// ABOUTME: Verifies thin MediatR dispatch, route metadata, and RFC 7807 failure mapping.

using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Explore.API.Controllers;
using Explore.API.Extensions;
using Explore.API.Hateoas;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.DTOs.StorageObject;
using Explore.Application.Features.StorageObjects.Requests.Commands;
using Explore.Application.Features.StorageObjects.Requests.Queries;
using Explore.Application.Models.Storage;
using Explore.Application.Responses;
using Explore.Domain;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.RateLimiting;
using NSubstitute;

namespace Event.Api.IntegrationTests.Features;

public sealed class StorageUploadSessionControllerTests
{
    private readonly Guid _tenantId = Guid.CreateVersion7();
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();

    public StorageUploadSessionControllerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
    }

    [Test]
    public async Task CreateUploadSession_DispatchesCommandWithTenantContext()
    {
        var dto = CreateDto();
        var response = Success(Guid.CreateVersion7());
        _mediator.Send(Arg.Any<CreateStorageUploadSessionCommand>(), Arg.Any<CancellationToken>())
            .Returns(response);
        var controller = CreateController();

        var actionResult = await controller.CreateUploadSession(dto, CancellationToken.None);

        var ok = actionResult.Result as OkObjectResult;
        await Assert.That(ok).IsNotNull();
        await Assert.That(ok!.Value).IsEqualTo(response);
        await _mediator.Received(1).Send(
            Arg.Is<CreateStorageUploadSessionCommand>(command =>
                command.UploadSessionDto == dto &&
                command.TenantId == _tenantId),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task UploadSessionContent_DispatchesStreamCommandWithoutBrowserDestination()
    {
        var sessionId = Guid.CreateVersion7();
        var body = new MemoryStream(Encoding.UTF8.GetBytes("hello"));
        var response = Success(sessionId);
        _mediator.Send(Arg.Any<FinalizeStorageUploadSessionCommand>(), Arg.Any<CancellationToken>())
            .Returns(response);
        var controller = CreateController(body, "text/plain", body.Length);

        var actionResult = await controller.UploadSessionContent(sessionId, CancellationToken.None);

        var ok = actionResult.Result as OkObjectResult;
        await Assert.That(ok).IsNotNull();
        await _mediator.Received(1).Send(
            Arg.Is<FinalizeStorageUploadSessionCommand>(command =>
                command.UploadSessionId == sessionId &&
                command.Content == body &&
                command.ContentType == "text/plain" &&
                command.ContentLength == body.Length &&
                command.TenantId == _tenantId),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CancelUploadSession_DispatchesCommandWithTenantContext()
    {
        var sessionId = Guid.CreateVersion7();
        var response = Success(sessionId);
        _mediator.Send(Arg.Any<CancelStorageUploadSessionCommand>(), Arg.Any<CancellationToken>())
            .Returns(response);
        var controller = CreateController();

        var actionResult = await controller.CancelUploadSession(sessionId, CancellationToken.None);

        var ok = actionResult.Result as OkObjectResult;
        await Assert.That(ok).IsNotNull();
        await _mediator.Received(1).Send(
            Arg.Is<CancelStorageUploadSessionCommand>(command =>
                command.UploadSessionId == sessionId &&
                command.TenantId == _tenantId),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetContent_DispatchesMetadataIdQueryAndEnablesRangeProcessing()
    {
        var storageObjectId = Guid.CreateVersion7();
        var response = new StorageObjectContentResult(
            new MemoryStream([1, 2, 3]),
            "image/png",
            3,
            DateTimeOffset.UtcNow,
            "abc123");
        _mediator.Send(Arg.Any<GetStorageObjectContentRequest>(), Arg.Any<CancellationToken>())
            .Returns(response);
        var controller = CreateController();

        var actionResult = await controller.GetContent(storageObjectId, CancellationToken.None);

        var file = actionResult as FileStreamResult;
        await Assert.That(file).IsNotNull();
        await Assert.That(file!.EnableRangeProcessing).IsTrue();
        await Assert.That(file.ContentType).IsEqualTo("image/png");
        await Assert.That(file.EntityTag?.Tag).IsEqualTo("\"abc123\"");
        await _mediator.Received(1).Send(
            Arg.Is<GetStorageObjectContentRequest>(query => query.StorageObjectId == storageObjectId),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetPublicImage_WhenReaderReturnsNull_ReturnsNotFound()
    {
        _mediator.Send(Arg.Any<GetPublicImageRequest>(), Arg.Any<CancellationToken>())
            .Returns((StorageObjectContentResult?)null);
        var controller = CreateController();

        var actionResult = await controller.GetPublicImage(Guid.CreateVersion7(), CancellationToken.None);

        await Assert.That(actionResult).IsTypeOf<NotFoundResult>();
    }

    [Test]
    public async Task CreateUploadSession_WhenTooLarge_ReturnsPayloadTooLargeProblemDetails()
    {
        _mediator.Send(Arg.Any<CreateStorageUploadSessionCommand>(), Arg.Any<CancellationToken>())
            .Returns(Failure("Upload exceeds the configured per-file limit.", FailureCodes.StorageUploadTooLarge));
        var controller = CreateController();

        var actionResult = await controller.CreateUploadSession(CreateDto(), CancellationToken.None);

        var content = actionResult.Result as ContentResult;
        await Assert.That(content).IsNotNull();
        await Assert.That(content!.StatusCode).IsEqualTo((int)HttpStatusCode.RequestEntityTooLarge);
        await Assert.That(content.ContentType).IsEqualTo("application/problem+json");

        using var document = JsonDocument.Parse(content.Content!);
        await Assert.That(document.RootElement.GetProperty("code").GetString()).IsEqualTo(FailureCodes.StorageUploadTooLarge);
        await Assert.That(document.RootElement.GetProperty("title").GetString()).IsEqualTo("Storage upload is too large");
    }

    [Test]
    public async Task UploadSessionRoutes_UseStableRouteNamesAndWritePolicy()
    {
        var create = typeof(StorageObjectController).GetMethod(nameof(StorageObjectController.CreateUploadSession))!;
        var upload = typeof(StorageObjectController).GetMethod(nameof(StorageObjectController.UploadSessionContent))!;
        var cancel = typeof(StorageObjectController).GetMethod(nameof(StorageObjectController.CancelUploadSession))!;
        var content = typeof(StorageObjectController).GetMethod(nameof(StorageObjectController.GetContent))!;

        await AssertRoute(create, typeof(HttpPostAttribute), "upload-sessions", RouteNames.CreateStorageUploadSession);
        await AssertRoute(upload, typeof(HttpPutAttribute), "upload-sessions/{uploadSessionId:guid}/content", RouteNames.UploadStorageUploadSessionContent);
        await AssertRoute(cancel, typeof(HttpDeleteAttribute), "upload-sessions/{uploadSessionId:guid}", RouteNames.CancelStorageUploadSession);
        await AssertRoute(content, typeof(HttpGetAttribute), "{id:guid}/content", RouteNames.GetStorageObjectContent);

        await Assert.That(GetRateLimitPolicy(create)).IsEqualTo(RateLimitingExtensions.WritePolicy);
        await Assert.That(GetRateLimitPolicy(upload)).IsEqualTo(RateLimitingExtensions.WritePolicy);
        await Assert.That(GetRateLimitPolicy(cancel)).IsEqualTo(RateLimitingExtensions.WritePolicy);
        await Assert.That(GetRequestTimeoutPolicy(upload)).IsEqualTo(RequestTimeoutExtensions.ComplexPolicy);
    }

    [Test]
    public async Task ArbitraryKeyStorageRoutes_AreNotExposed()
    {
        var routeTemplates = typeof(StorageObjectController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .SelectMany(method => method.GetCustomAttributes<HttpMethodAttribute>())
            .Select(attribute => attribute.Template)
            .Where(template => template is not null)
            .ToArray();
        var routeNames = typeof(RouteNames)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.FieldType == typeof(string))
            .Select(field => (string)field.GetValue(null)!)
            .ToArray();

        await Assert.That(routeTemplates).DoesNotContain("file/{*fileKey}");
        await Assert.That(routeTemplates).DoesNotContain("presigned-url-by-key/{*objectKey}");
        await Assert.That(routeNames).DoesNotContain("GetStorageObjectFile");
        await Assert.That(routeNames).DoesNotContain("GetStorageObjectPresignedDownloadUrlByKey");
    }

    private StorageObjectController CreateController(Stream? requestBody = null, string? contentType = null, long? contentLength = null)
    {
        var httpContext = new DefaultHttpContext
        {
            TraceIdentifier = "trace-test"
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, Guid.CreateVersion7().ToString())], "test"));
        httpContext.Request.Body = requestBody ?? Stream.Null;
        httpContext.Request.ContentType = contentType;
        httpContext.Request.ContentLength = contentLength;

        return new StorageObjectController(_mediator, _tenantContext)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
    }

    private static async Task AssertRoute(MethodInfo method, Type attributeType, string template, string routeName)
    {
        var attribute = method.GetCustomAttributes().Single(value => value.GetType() == attributeType) as HttpMethodAttribute;
        await Assert.That(attribute).IsNotNull();
        await Assert.That(attribute!.Template).IsEqualTo(template);
        await Assert.That(attribute.Name).IsEqualTo(routeName);
    }

    private static string? GetRateLimitPolicy(MethodInfo method)
        => method.GetCustomAttribute<EnableRateLimitingAttribute>()?.PolicyName;

    private static string? GetRequestTimeoutPolicy(MethodInfo method)
        => method.GetCustomAttribute<RequestTimeoutAttribute>()?.PolicyName;

    private static CreateStorageUploadSessionDto CreateDto()
        => new()
        {
            ExpectedSizeBytes = 5,
            ContentType = "text/plain",
            OriginalFileName = "hello.txt",
            Purpose = StorageObjectPurposes.Attachment,
            Visibility = StorageObjectVisibilities.PrivateOwner,
            IdempotencyKey = "upload-test"
        };

    private static BaseCommandResponse<StorageUploadSessionDto> Success(Guid sessionId)
        => new()
        {
            Success = true,
            Message = "ok",
            Id = new StorageUploadSessionDto
            {
                Id = sessionId,
                TenantId = Guid.CreateVersion7(),
                Provider = StorageProviders.Local,
                ContentType = "text/plain",
                SafeDisplayName = "hello.txt",
                Purpose = StorageObjectPurposes.Attachment,
                Visibility = StorageObjectVisibilities.PrivateOwner,
                Status = StorageUploadSessionStates.Reserved
            }
        };

    private static BaseCommandResponse<StorageUploadSessionDto> Failure(string message, string failureCode)
        => new()
        {
            Success = false,
            Message = message,
            FailureCode = failureCode,
            Errors = [message]
        };
}
