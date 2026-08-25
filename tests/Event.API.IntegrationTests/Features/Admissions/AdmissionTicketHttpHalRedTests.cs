// ABOUTME: Live TestServer RED contracts for account ticket visibility, delivery, HAL, and headers.
// ABOUTME: Exercises active-to-revoked state changes through actual HTTP responses.

using System.Net;
using Microsoft.AspNetCore.Http;

namespace Event.Api.IntegrationTests.Features;

public sealed partial class AdmissionTicketApiRedContractTests
{
    [Test]
    public async Task AccountTicketListContainsOnlyCurrentAccountTickets()
    {
        await RequireRoute(AccountList);
        var scenario = new AdmissionApiScenario();
        await using var factory = new AdmissionApiFactory(scenario);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage list = await SendAccountGet(client, "/api/tickets");
        string body = await list.Content.ReadAsStringAsync();

        await Assert.That(list.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(body.Contains(
            scenario.AccountTicketId.ToString("D"), StringComparison.OrdinalIgnoreCase)).IsTrue();
        await Assert.That(body.Contains(
            scenario.CrossTenantTicketId.ToString("D"), StringComparison.OrdinalIgnoreCase)).IsFalse();
    }

    [Test]
    public async Task AccountDetailHidesCrossTenantTicketsAsGenericAbsence()
    {
        await RequireRoute(AccountDetail);
        var scenario = new AdmissionApiScenario();
        await using var factory = new AdmissionApiFactory(scenario);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage own = await SendAccountGet(client, AccountPath(scenario.AccountTicketId));
        string ownBody = await own.Content.ReadAsStringAsync();
        using HttpResponseMessage crossTenant = await SendAccountGet(
            client, AccountPath(scenario.CrossTenantTicketId));
        using HttpResponseMessage absent = await SendAccountGet(client, AccountPath(scenario.AbsentTicketId));

        await Assert.That(own.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await AssertTicketMetadata(
            ownBody,
            scenario.AccountTicketId,
            scenario.EventId,
            scenario.ActiveStatusCode,
            scenario.AccountDisplayReference);
        await Assert.That(crossTenant.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
        await Assert.That(absent.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
        await Assert.That(await ProblemFingerprint(crossTenant))
            .IsEqualTo(await ProblemFingerprint(absent));
    }

    [Test]
    public async Task RevokedTicketRemainsVisibleWhileCredentialDeliveriesFailClosed()
    {
        await RequireRoute(AccountDetail);
        await RequireRoute(AccountQr);
        await RequireRoute(AccountPrint);
        var scenario = new AdmissionApiScenario();
        scenario.RevokeAccountTicket();
        await using var factory = new AdmissionApiFactory(scenario);
        using HttpClient client = factory.CreateClient();
        string accountPath = AccountPath(scenario.AccountTicketId);

        using HttpResponseMessage detail = await SendAccountGet(client, accountPath);
        string body = await detail.Content.ReadAsStringAsync();
        using HttpResponseMessage qr = await SendAccountGet(client, accountPath + "/qr");
        using HttpResponseMessage print = await SendAccountGet(client, accountPath + "/print");

        await Assert.That(detail.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await AssertTicketMetadata(
            body,
            scenario.AccountTicketId,
            scenario.EventId,
            "REVOKED",
            scenario.AccountDisplayReference);
        await Assert.That(Relations(body)).DoesNotContain(QrRelation);
        await Assert.That(Relations(body)).DoesNotContain(PrintRelation);
        await Assert.That(qr.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
        await Assert.That(print.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
        await Assert.That(await ProblemFingerprint(qr)).IsEqualTo(await ProblemFingerprint(print));
    }

    [Test]
    public async Task DirectEndpointsAndHalRelationsStayInParityAfterRevocation()
    {
        await RequireRoute(AccountDetail);
        await RequireRoute(AccountQr);
        await RequireRoute(AccountPrint);
        var scenario = new AdmissionApiScenario();
        await using var factory = new AdmissionApiFactory(scenario);
        using HttpClient client = factory.CreateClient();
        string accountPath = AccountPath(scenario.AccountTicketId);

        using HttpResponseMessage activeDetail = await SendAccountGet(client, accountPath);
        string activeBody = await activeDetail.Content.ReadAsStringAsync();
        using HttpResponseMessage activeQr = await SendAccountGet(client, accountPath + "/qr");
        string activeQrBody = await activeQr.Content.ReadAsStringAsync();
        using HttpResponseMessage activePrint = await SendAccountGet(client, accountPath + "/print");
        string activePrintBody = await activePrint.Content.ReadAsStringAsync();

        await AssertHalLink(activeBody, QrRelation, accountPath + "/qr");
        await AssertHalLink(activeBody, PrintRelation, accountPath + "/print");
        await Assert.That(activeQr.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(activeQrBody.Contains(scenario.ManualCredential, StringComparison.Ordinal)).IsTrue();
        await Assert.That(activeQrBody.Contains("SENSITIVE", StringComparison.OrdinalIgnoreCase)).IsTrue();
        await Assert.That(activePrint.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await AssertPrivateNoReferrer(activePrint);
        await AssertTicketMetadata(
            activePrintBody,
            scenario.AccountTicketId,
            scenario.EventId,
            scenario.ActiveStatusCode,
            scenario.AccountDisplayReference);
        await Assert.That(JsonString(activePrintBody, "manualCode")).IsEqualTo(scenario.ManualCredential);
        await Assert.That(JsonString(activePrintBody, "manualCodeClassificationCode"))
            .IsEqualTo(scenario.SensitiveClassification);
        await Assert.That(JsonString(activePrintBody, "qrRepresentation"))
            .IsEqualTo(scenario.QrRepresentation);
        await Assert.That(JsonString(activePrintBody, "printModel")).IsEqualTo(scenario.PrintModel);

        scenario.RevokeAccountTicket();
        using HttpResponseMessage revokedDetail = await SendAccountGet(client, accountPath);
        string revokedBody = await revokedDetail.Content.ReadAsStringAsync();
        using HttpResponseMessage revokedQr = await SendAccountGet(client, accountPath + "/qr");
        using HttpResponseMessage revokedPrint = await SendAccountGet(client, accountPath + "/print");

        await Assert.That(Relations(revokedBody)).DoesNotContain(QrRelation);
        await Assert.That(Relations(revokedBody)).DoesNotContain(PrintRelation);
        await Assert.That(revokedQr.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
        await Assert.That(revokedPrint.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    [Arguments("account-detail")]
    [Arguments("account-qr")]
    [Arguments("account-print")]
    public async Task SensitiveTicketResponsesSetActualNoStoreAndNoReferrerHeaders(string surface)
    {
        await RequireRoute(SurfaceRoute(surface));
        var scenario = new AdmissionApiScenario();
        await using var factory = new AdmissionApiFactory(scenario);
        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage response = await SendAccountGet(
            client, AccountSurfacePath(scenario.AccountTicketId, surface));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await AssertPrivateNoReferrer(response);
    }

    private static async Task AssertTicketMetadata(
        string body,
        Guid ticketId,
        Guid eventId,
        string statusCode,
        string displayReference)
    {
        await Assert.That(JsonGuid(body, "ticketId")).IsEqualTo(ticketId);
        await Assert.That(JsonGuid(body, "eventId")).IsEqualTo(eventId);
        await Assert.That(JsonString(body, "statusCode")).IsEqualTo(statusCode);
        await Assert.That(JsonString(body, "displayReference")).IsEqualTo(displayReference);
    }

    private static async Task AssertHalLink(string body, string relation, string expectedHref)
    {
        await Assert.That(Relations(body)).Contains(relation);
        await Assert.That(LinkHref(body, relation)).IsEqualTo(expectedHref);
        await Assert.That(LinkMethod(body, relation)).IsEqualTo(HttpMethods.Get);
    }
}
