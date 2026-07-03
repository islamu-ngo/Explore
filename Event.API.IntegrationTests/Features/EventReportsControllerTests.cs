// ABOUTME: API contract tests for reporter-facing event-report endpoints.
// ABOUTME: Verifies route metadata, ProblemDetails mapping, and API-boundary fingerprint hashing.

using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Explore.API.Attributes;
using Explore.API.Controllers;
using Explore.API.Extensions;
using Explore.API.Hateoas;
using Explore.Application.Contracts.Hateoas;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.DTOs.EventReporting;
using Explore.Application.Features.EventReporting;
using Explore.Application.Features.EventReporting.Requests.Commands;
using Explore.Application.Features.EventReporting.Requests.Queries;
using Explore.Application.Hateoas;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Event.Api.IntegrationTests.Features;

public sealed class EventReportsControllerTests
{
    private const string ReporterFingerprintPepper = "reporting-pepper";

    private readonly Guid _tenantId = Guid.CreateVersion7();
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly IResourceAssembler<EventReportOptionsDto, EventReportOptionsDto> _optionsAssembler =
        Substitute.For<IResourceAssembler<EventReportOptionsDto, EventReportOptionsDto>>();
    private readonly IResourceAssembler<MyEventReportDto, MyEventReportDto> _myReportAssembler =
        Substitute.For<IResourceAssembler<MyEventReportDto, MyEventReportDto>>();

    public EventReportsControllerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
    }

    [Test]
    public async Task Routes_UseStableNamesAuthorizationAndRateLimitPolicies()
    {
        var getOptions = typeof(EventReportsController).GetMethod(nameof(EventReportsController.GetOptions))!;
        var submit = typeof(EventReportsController).GetMethod(nameof(EventReportsController.Submit))!;
        var getMyReports = typeof(EventReportsController).GetMethod(nameof(EventReportsController.GetMyReports))!;
        var getMyReport = typeof(EventReportsController).GetMethod(nameof(EventReportsController.GetMyReport))!;

        await AssertRoute(getOptions, typeof(HttpGetAttribute), "events/{eventId:guid}/options", RouteNames.GetEventReportOptions);
        await Assert.That(getOptions.GetCustomAttribute<AllowAnonymousAttribute>()).IsNotNull();
        await Assert.That(getOptions.GetCustomAttribute<AuthorizeAttribute>()).IsNull();
        await Assert.That(getOptions.GetCustomAttribute<EndpointClassificationAttribute>()?.Class).IsEqualTo(EndpointClass.Public);
        await Assert.That(getOptions.GetCustomAttribute<OutputCacheAttribute>()?.PolicyName).IsEqualTo("DetailData");

        await AssertRoute(submit, typeof(HttpPostAttribute), null, RouteNames.SubmitEventReport);
        await Assert.That(submit.GetCustomAttribute<AuthorizeAttribute>()).IsNotNull();
        await Assert.That(submit.GetCustomAttribute<AllowAnonymousAttribute>()).IsNull();
        await Assert.That(submit.GetCustomAttribute<EndpointClassificationAttribute>()?.Class).IsEqualTo(EndpointClass.Authenticated);
        await Assert.That(GetRateLimitPolicy(submit)).IsEqualTo(RateLimitingExtensions.WritePolicy);

        await AssertRoute(getMyReports, typeof(HttpGetAttribute), "my", RouteNames.GetMyEventReports);
        await Assert.That(getMyReports.GetCustomAttribute<AuthorizeAttribute>()).IsNotNull();
        await Assert.That(getMyReports.GetCustomAttribute<AllowAnonymousAttribute>()).IsNull();
        await Assert.That(getMyReports.GetCustomAttribute<EndpointClassificationAttribute>()?.Class).IsEqualTo(EndpointClass.Authenticated);
        await Assert.That(GetRateLimitPolicy(getMyReports)).IsEqualTo(RateLimitingExtensions.AuthenticatedPolicy);

        await AssertRoute(getMyReport, typeof(HttpGetAttribute), "my/{reportId:guid}", RouteNames.GetMyEventReport);
        await Assert.That(getMyReport.GetCustomAttribute<AuthorizeAttribute>()).IsNotNull();
        await Assert.That(getMyReport.GetCustomAttribute<AllowAnonymousAttribute>()).IsNull();
        await Assert.That(getMyReport.GetCustomAttribute<EndpointClassificationAttribute>()?.Class).IsEqualTo(EndpointClass.Authenticated);
        await Assert.That(GetRateLimitPolicy(getMyReport)).IsEqualTo(RateLimitingExtensions.AuthenticatedPolicy);
    }

    [Test]
    public async Task GetMyReports_DispatchesPagedReporterQueryAndReturnsHalCollection()
    {
        var result = new PaginatedResult<MyEventReportDto>
        {
            Items = [CreateMyReportDto()],
            PageNumber = 2,
            PageSize = 10,
            TotalCount = 12
        };
        var halCollection = HalCollectionResource<MyEventReportDto>.Create(
            [new HalResource<MyEventReportDto>(result.Items[0])],
            result.PageNumber,
            result.PageSize,
            result.TotalCount,
            []);
        _mediator.Send(Arg.Any<GetMyReportsRequest>(), Arg.Any<CancellationToken>())
            .Returns(result);
        _myReportAssembler.ToCollectionResource(
                Arg.Is<PaginatedResult<MyEventReportDto>>(value => ReferenceEquals(value, result)),
                Arg.Is<string>(value => value == RouteNames.GetMyEventReports),
                Arg.Is<object?>(value => value == null),
                Arg.Any<HttpContext>())
            .Returns(halCollection);
        var controller = CreateController("203.0.113.5", "EventReportingTests/1.0", "corr-report-list");

        var actionResult = await controller.GetMyReports(2, 10, CancellationToken.None);

        var ok = actionResult.Result as OkObjectResult;
        await Assert.That(ok).IsNotNull();
        await Assert.That(ok!.Value).IsEqualTo(halCollection);
        await _mediator.Received(1).Send(
            Arg.Is<GetMyReportsRequest>(query => query.PageNumber == 2 && query.PageSize == 10),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Submit_HashesReporterFingerprintsBeforeDispatchingCommand()
    {
        var reportId = Guid.CreateVersion7();
        var dto = CreateSubmitDto();
        _mediator.Send(Arg.Any<SubmitEventReportCommand>(), Arg.Any<CancellationToken>())
            .Returns(new BaseCommandResponse<Guid>
            {
                Success = true,
                Id = reportId,
                Message = "Event report submitted successfully."
            });
        var controller = CreateController("203.0.113.5", "EventReportingTests/1.0", "corr-report-1");

        var actionResult = await controller.Submit(dto, CancellationToken.None);

        var created = actionResult.Result as CreatedAtRouteResult;
        await Assert.That(created).IsNotNull();
        await Assert.That(created!.RouteName).IsEqualTo(RouteNames.GetMyEventReport);
        await Assert.That(created.RouteValues!["reportId"]).IsEqualTo(reportId);

        var expectedIpHash = ComputeExpectedFingerprint("ip", "203.0.113.5");
        var expectedUserAgentHash = ComputeExpectedFingerprint("user-agent", "EventReportingTests/1.0");
        await _mediator.Received(1).Send(
            Arg.Is<SubmitEventReportCommand>(command =>
                ReferenceEquals(command.Request, dto) &&
                command.ReporterIpHash == expectedIpHash &&
                command.ReporterUserAgentHash == expectedUserAgentHash &&
                command.ReporterIpHash != "203.0.113.5" &&
                command.ReporterUserAgentHash != "EventReportingTests/1.0" &&
                command.CorrelationId == "corr-report-1"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Submit_WhenDuplicate_ReturnsConflictProblemDetails()
    {
        _mediator.Send(Arg.Any<SubmitEventReportCommand>(), Arg.Any<CancellationToken>())
            .Returns(new BaseCommandResponse<Guid>
            {
                Success = false,
                Message = "A matching event report was already submitted recently.",
                FailureCode = EventReportFailureCodes.Duplicate,
                Errors = ["A matching report already exists in the duplicate prevention window."]
            });
        var controller = CreateController("203.0.113.5", "EventReportingTests/1.0", "corr-report-2");

        var actionResult = await controller.Submit(CreateSubmitDto(), CancellationToken.None);

        var objectResult = actionResult.Result as ObjectResult;
        await Assert.That(objectResult).IsNotNull();
        await Assert.That(objectResult!.StatusCode).IsEqualTo(StatusCodes.Status409Conflict);
        var problemDetails = objectResult.Value as ProblemDetails;
        await Assert.That(problemDetails).IsNotNull();
        await Assert.That(problemDetails!.Title).IsEqualTo("Event report conflict");
        await Assert.That(problemDetails.Extensions["code"]).IsEqualTo(EventReportFailureCodes.Duplicate);
    }

    private EventReportsController CreateController(string? ipAddress, string? userAgent, string correlationId)
    {
        var httpContext = new DefaultHttpContext
        {
            TraceIdentifier = "trace-reporting-test"
        };
        httpContext.Items["CorrelationId"] = correlationId;
        if (!string.IsNullOrWhiteSpace(ipAddress))
        {
            httpContext.Connection.RemoteIpAddress = IPAddress.Parse(ipAddress);
        }

        if (!string.IsNullOrWhiteSpace(userAgent))
        {
            httpContext.Request.Headers.UserAgent = userAgent;
        }

        return new EventReportsController(
            _mediator,
            _optionsAssembler,
            _myReportAssembler,
            _tenantContext,
            Options.Create(new EventReportSubmissionOptions
            {
                ReporterFingerprintPepper = ReporterFingerprintPepper
            }),
            NullLogger<EventReportsController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
    }

    private string ComputeExpectedFingerprint(string kind, string value)
    {
        var material = $"{_tenantId:N}:{kind}:{value}";
        var hash = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(ReporterFingerprintPepper),
            Encoding.UTF8.GetBytes(material));

        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static SubmitEventReportDto CreateSubmitDto() => new()
    {
        EventId = Guid.CreateVersion7(),
        ReasonCode = "spam",
        ReporterText = "This event appears to be spam.",
        ReporterContactConsent = true,
        ReporterLocale = "en"
    };

    private static MyEventReportDto CreateMyReportDto() => new()
    {
        Id = Guid.CreateVersion7(),
        EventId = Guid.CreateVersion7(),
        StatusId = 1,
        StatusCode = "submitted",
        StatusName = "Submitted",
        ReasonCode = "spam",
        ReasonName = "Spam",
        SubmittedAtUtc = DateTime.UtcNow,
        ReporterContactConsent = true
    };

    private static async Task AssertRoute(MethodInfo method, Type attributeType, string? template, string routeName)
    {
        var attribute = method.GetCustomAttributes().Single(value => value.GetType() == attributeType) as HttpMethodAttribute;
        await Assert.That(attribute).IsNotNull();
        await Assert.That(attribute!.Template).IsEqualTo(template);
        await Assert.That(attribute.Name).IsEqualTo(routeName);
    }

    private static string? GetRateLimitPolicy(MethodInfo method)
        => method.GetCustomAttribute<EnableRateLimitingAttribute>()?.PolicyName;
}
