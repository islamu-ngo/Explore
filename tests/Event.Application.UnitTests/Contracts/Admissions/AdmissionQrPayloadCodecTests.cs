// ABOUTME: Specifies the exact admission QR payload grammar and redacted bearer value semantics.
// ABOUTME: Proves one canonical codec round-trips v1 material and rejects malformed input without echoing it.

using System.Diagnostics;
using Event.Wire.Contracts.Admissions;

namespace Event.Application.UnitTests.Contracts.Admissions;

public sealed class AdmissionQrPayloadCodecTests
{
    private const string Bearer = "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8";
    private const string PayloadText = "islamu-admission:v1:" + Bearer;

    [Test]
    public async Task ExactV1PayloadRoundTrips()
    {
        bool bearerAccepted = AdmissionCredentialBearer.TryCreate(Bearer, out AdmissionCredentialBearer? bearer);
        string encoded = AdmissionQrPayloadCodec.Encode(bearer!);
        bool payloadAccepted = AdmissionQrPayloadCodec.TryDecode(encoded, out AdmissionQrPayload? payload);

        await Assert.That(bearerAccepted).IsTrue();
        await Assert.That(encoded).IsEqualTo(PayloadText);
        await Assert.That(encoded.Length).IsEqualTo(63);
        await Assert.That(payloadAccepted).IsTrue();
        await Assert.That(payload!.Bearer.Value).IsEqualTo(Bearer);
    }

    [Test]
    [Arguments("")]
    [Arguments("islamu-admission:v2:AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8")]
    [Arguments("ISLAMU-admission:v1:AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8")]
    [Arguments("islamu-admission:v1:AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8=")]
    [Arguments("islamu-admission:v1: AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8")]
    [Arguments("islamu-admission:v1:AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh+")]
    [Arguments("islamu-admission:v1:AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh")]
    [Arguments("islamu-admission:v1:___________________________________________")]
    public async Task MalformedAndUnknownPayloadsFailClosed(string candidate)
    {
        bool accepted = AdmissionQrPayloadCodec.TryDecode(candidate, out AdmissionQrPayload? payload);

        await Assert.That(accepted).IsFalse();
        await Assert.That(payload).IsNull();
    }

    [Test]
    public async Task TokenBearingValuesRedactStringRepresentations()
    {
        AdmissionCredentialBearer.TryCreate(Bearer, out AdmissionCredentialBearer? bearer);
        AdmissionQrPayloadCodec.TryDecode(PayloadText, out AdmissionQrPayload? payload);

        string bearerDebugger = typeof(AdmissionCredentialBearer)
            .GetCustomAttributes(typeof(DebuggerDisplayAttribute), false)
            .Cast<DebuggerDisplayAttribute>()
            .Single().Value;
        string payloadDebugger = typeof(AdmissionQrPayload)
            .GetCustomAttributes(typeof(DebuggerDisplayAttribute), false)
            .Cast<DebuggerDisplayAttribute>()
            .Single().Value;

        await Assert.That(bearer!.ToString()).DoesNotContain(Bearer);
        await Assert.That(payload!.ToString()).DoesNotContain(Bearer);
        await Assert.That(bearerDebugger).DoesNotContain(Bearer);
        await Assert.That(payloadDebugger).DoesNotContain(Bearer);
    }
}
