// ABOUTME: Tests hardened bounded ATProto repository snapshot retrieval and canonical materialization.
// ABOUTME: Covers DID/PDS binding, CAR integrity, presence safety, and transport limits.

using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using CarpaNet;
using CarpaNet.Cbor;
using Explore.Atproto.Transport;
using Explore.Domain;
using Explore.Infrastructure.Services.Federation;

namespace Explore.Infrastructure.Tests.Federation;

public sealed class AtprotoPdsSnapshotGatewayTests
{
    private const string Did = "did:plc:snapshot-owner";
    private const long SnapshotVersion = 1_768_212_000_000_000;

    [Test]
    public async Task FetchAsync_ValidRepository_UsesExactPublicGetRepoAndMaterializesCanonicalEvent()
    {
        var transport = new SnapshotTransport(Did, "https://pds.example", SnapshotCar.Create(Did));
        var gateway = new AtprotoPdsSnapshotGateway(transport.CreatePrimaryHandler);

        var result = await gateway.FetchAsync(Did, SnapshotVersion, CancellationToken.None);

        await Assert.That(result.IsComplete).IsTrue();
        await Assert.That(result.FailureCode).IsNull();
        await Assert.That(result.Snapshot!.Did).IsEqualTo(Did);
        await Assert.That(result.Snapshot.PresentIdentities).HasSingleItem();
        await Assert.That(result.Snapshot.Items).HasSingleItem();
        await Assert.That(result.Snapshot.Items[0].Record.Direction).IsEqualTo(AtprotoRecordDirection.Inbound);
        await Assert.That(result.Snapshot.Items[0].Record.SourceVersion).IsEqualTo(SnapshotVersion);
        await Assert.That(result.Snapshot.Items[0].Record.SourceCursor).IsNull();
        await Assert.That(result.Snapshot.Items[0].EventProjection!.Name).IsEqualTo("Recovered event");
        await Assert.That(transport.PdsRequests).IsEqualTo(1);
        await Assert.That(transport.LastPdsRequest!.Method).IsEqualTo(HttpMethod.Get);
        await Assert.That(transport.LastPdsRequest.RequestUri!.AbsolutePath)
            .IsEqualTo("/xrpc/com.atproto.sync.getRepo");
        await Assert.That(transport.LastPdsRequest.RequestUri.Query).IsEqualTo("?did=did%3Aplc%3Asnapshot-owner");
        await Assert.That(transport.LastPdsRequest.Headers.Authorization).IsNull();
    }

    [Test]
    public async Task FetchAsync_ContentRejectedTargetRecord_RetainsPresenceWithoutMaterializingIt()
    {
        var gateway = Gateway(SnapshotCar.Create(Did, recordType: "app.example.wrong"));

        var result = await gateway.FetchAsync(Did, SnapshotVersion, CancellationToken.None);

        await Assert.That(result.IsComplete).IsTrue();
        await Assert.That(result.Snapshot!.PresentIdentities).HasSingleItem();
        await Assert.That(result.Snapshot.Items).IsEmpty();
    }

    [Test]
    public async Task FetchAsync_EventAndRsvpRepository_MaterializesBothExactCollectionsAndSubjectBinding()
    {
        var result = await Gateway(SnapshotCar.Create(Did, includeRsvp: true))
            .FetchAsync(Did, SnapshotVersion, CancellationToken.None);

        await Assert.That(result.IsComplete).IsTrue();
        await Assert.That(result.Snapshot!.PresentIdentities).Count().IsEqualTo(2);
        await Assert.That(result.Snapshot.Items).Count().IsEqualTo(2);
        var rsvp = result.Snapshot.Items.Single(item =>
            item.Record.Collection == "community.lexicon.calendar.rsvp");
        await Assert.That(rsvp.EventProjection).IsNull();
        await Assert.That(rsvp.Record.SubjectUri)
            .IsEqualTo($"at://{Did}/community.lexicon.calendar.event/3m-recovered");
        await Assert.That(rsvp.Record.SubjectCid).IsNotNull();
    }

    [Test]
    public async Task FetchAsync_RepositoryDidMismatch_FailsWholeSnapshot()
    {
        var result = await Gateway(SnapshotCar.Create("did:plc:different-owner"))
            .FetchAsync(Did, SnapshotVersion, CancellationToken.None);

        await Assert.That(result.IsComplete).IsFalse();
        await Assert.That(result.Snapshot).IsNull();
        await Assert.That(result.FailureCode).IsEqualTo("repository_identity_mismatch");
    }

    [Test]
    public async Task FetchAsync_BlockContentDoesNotMatchCid_FailsWholeSnapshot()
    {
        var result = await Gateway(SnapshotCar.Create(Did, tamperRecordBlock: true))
            .FetchAsync(Did, SnapshotVersion, CancellationToken.None);

        await Assert.That(result.IsComplete).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("repository_integrity_invalid");
    }

    [Test]
    public async Task FetchAsync_MissingReferencedRecordBlock_FailsWholeSnapshot()
    {
        var result = await Gateway(SnapshotCar.Create(Did, omitRecordBlock: true))
            .FetchAsync(Did, SnapshotVersion, CancellationToken.None);

        await Assert.That(result.IsComplete).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("repository_incomplete");
    }

    [Test]
    public async Task FetchAsync_TruncatedCar_FailsBeforeRepositoryAllocation()
    {
        byte[] complete = SnapshotCar.Create(Did);
        var result = await Gateway(complete[..^1])
            .FetchAsync(Did, SnapshotVersion, CancellationToken.None);

        await Assert.That(result.IsComplete).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("repository_framing_invalid");
    }

    [Test]
    public async Task FetchAsync_UnsafePdsEndpoint_FailsBeforePdsRequest()
    {
        var transport = new SnapshotTransport(Did, "https://127.0.0.1", SnapshotCar.Create(Did));
        var result = await new AtprotoPdsSnapshotGateway(transport.CreatePrimaryHandler)
            .FetchAsync(Did, SnapshotVersion, CancellationToken.None);

        await Assert.That(result.IsComplete).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("pds_endpoint_invalid");
        await Assert.That(transport.PdsRequests).IsEqualTo(0);
    }

    [Test]
    public async Task FetchAsync_DeclaredOversizeResponse_FailsBeforeBuffering()
    {
        var transport = new SnapshotTransport(Did, "https://pds.example", SnapshotCar.Create(Did))
        {
            DeclaredCarLength = AtprotoPdsSnapshotGateway.MaximumCarBytes + 1L
        };
        var result = await new AtprotoPdsSnapshotGateway(transport.CreatePrimaryHandler)
            .FetchAsync(Did, SnapshotVersion, CancellationToken.None);

        await Assert.That(result.IsComplete).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("repository_too_large");
    }

    [Test]
    public async Task SafetyBounds_AreExplicitAndFinite()
    {
        await Assert.That(AtprotoPdsSnapshotGateway.MaximumCarBytes).IsGreaterThan(0);
        await Assert.That(AtprotoPdsSnapshotGateway.MaximumBlocks).IsGreaterThan(0);
        await Assert.That(AtprotoPdsSnapshotGateway.MaximumTargetRecords).IsGreaterThan(0);
        await Assert.That(AtprotoPdsSnapshotGateway.RequestTimeout).IsGreaterThan(TimeSpan.Zero);
        await Assert.That(AtprotoPdsSnapshotGateway.RequestTimeout).IsLessThanOrEqualTo(TimeSpan.FromMinutes(1));
    }

    private static AtprotoPdsSnapshotGateway Gateway(byte[] car) => new(
        new SnapshotTransport(Did, "https://pds.example", car).CreatePrimaryHandler);

    private sealed class SnapshotTransport(string did, string pdsEndpoint, byte[] car)
    {
        public long? DeclaredCarLength { get; init; }
        public int PdsRequests { get; private set; }
        public HttpRequestMessage? LastPdsRequest { get; private set; }

        public DelegateHandler CreatePrimaryHandler(AtprotoOutboundPolicy _) =>
            new DelegateHandler(request =>
            {
                if (request.RequestUri!.Host == "plc.directory")
                {
                    string document = $$"""
                        {"id":"{{did}}","service":[{"id":"#atproto_pds","type":"AtprotoPersonalDataServer","serviceEndpoint":"{{pdsEndpoint}}"}]}
                        """;
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(document, Encoding.UTF8, "application/json")
                    };
                }

                PdsRequests++;
                LastPdsRequest = request;
                var content = new ByteArrayContent(car);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.ipld.car");
                if (DeclaredCarLength.HasValue)
                {
                    content.Headers.ContentLength = DeclaredCarLength.Value;
                }

                return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
            });
    }

    private sealed class DelegateHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(responder(request));
        }
    }

    private static class SnapshotCar
    {
        private const string Collection = "community.lexicon.calendar.event";
        private const string RecordKey = "3m-recovered";

        public static byte[] Create(
            string repositoryDid,
            string recordType = Collection,
            bool tamperRecordBlock = false,
            bool omitRecordBlock = false,
            bool includeRsvp = false)
        {
            byte[] record = Record(recordType);
            ATCid recordCid = Cid(record);
            var records = new List<(string Collection, string RecordKey, ATCid Cid, byte[] Data)>
            {
                (Collection, RecordKey, recordCid, record)
            };
            if (includeRsvp)
            {
                byte[] rsvp = Rsvp(repositoryDid, recordCid);
                records.Add(("community.lexicon.calendar.rsvp", "3m-rsvp", Cid(rsvp), rsvp));
            }

            byte[] mst = Mst(records);
            ATCid mstCid = Cid(mst);
            byte[] commit = Commit(repositoryDid, mstCid);
            ATCid commitCid = Cid(commit);
            byte[] header = Header(commitCid);
            if (tamperRecordBlock)
            {
                record[^1] ^= 0x01;
            }

            using var stream = new MemoryStream();
            WriteSection(stream, header);
            WriteBlock(stream, commitCid, commit);
            WriteBlock(stream, mstCid, mst);
            if (!omitRecordBlock)
            {
                foreach (var value in records)
                {
                    WriteBlock(stream, value.Cid, value.Data);
                }
            }

            return stream.ToArray();
        }

        private static byte[] Record(string type)
        {
            var writer = new DagCborWriter();
            writer.WriteStartMap(3);
            writer.WriteTextString("$type");
            writer.WriteTextString(type);
            writer.WriteTextString("name");
            writer.WriteTextString("Recovered event");
            writer.WriteTextString("createdAt");
            writer.WriteTextString("2026-01-12T10:00:00Z");
            writer.WriteEndMap();
            return writer.Encode();
        }

        private static byte[] Rsvp(string did, ATCid eventCid)
        {
            var writer = new DagCborWriter();
            writer.WriteStartMap(3);
            writer.WriteTextString("$type");
            writer.WriteTextString("community.lexicon.calendar.rsvp");
            writer.WriteTextString("status");
            writer.WriteTextString("community.lexicon.calendar.rsvp#going");
            writer.WriteTextString("subject");
            writer.WriteStartMap(2);
            writer.WriteTextString("uri");
            writer.WriteTextString($"at://{did}/{Collection}/{RecordKey}");
            writer.WriteTextString("cid");
            writer.WriteTextString(eventCid.Value);
            writer.WriteEndMap();
            writer.WriteEndMap();
            return writer.Encode();
        }

        private static byte[] Mst(
            IReadOnlyList<(string Collection, string RecordKey, ATCid Cid, byte[] Data)> records)
        {
            var writer = new DagCborWriter();
            writer.WriteStartMap(2);
            writer.WriteTextString("l");
            writer.WriteNull();
            writer.WriteTextString("e");
            writer.WriteStartArray(records.Count);
            foreach (var record in records)
            {
                writer.WriteStartMap(4);
                writer.WriteTextString("p");
                writer.WriteInt32(0);
                writer.WriteTextString("k");
                writer.WriteByteString(Encoding.UTF8.GetBytes($"{record.Collection}/{record.RecordKey}"));
                writer.WriteTextString("v");
                writer.WriteCidLink(record.Cid);
                writer.WriteTextString("t");
                writer.WriteNull();
                writer.WriteEndMap();
            }

            writer.WriteEndArray();
            writer.WriteEndMap();
            return writer.Encode();
        }

        private static byte[] Commit(string did, ATCid mstCid)
        {
            var writer = new DagCborWriter();
            writer.WriteStartMap(6);
            writer.WriteTextString("did");
            writer.WriteTextString(did);
            writer.WriteTextString("version");
            writer.WriteInt32(3);
            writer.WriteTextString("data");
            writer.WriteCidLink(mstCid);
            writer.WriteTextString("rev");
            writer.WriteTextString("3mrecovered000");
            writer.WriteTextString("prev");
            writer.WriteNull();
            writer.WriteTextString("sig");
            writer.WriteByteString(new byte[64]);
            writer.WriteEndMap();
            return writer.Encode();
        }

        private static byte[] Header(ATCid rootCid)
        {
            var writer = new DagCborWriter();
            writer.WriteStartMap(2);
            writer.WriteTextString("version");
            writer.WriteInt32(1);
            writer.WriteTextString("roots");
            writer.WriteStartArray(1);
            writer.WriteCidLink(rootCid);
            writer.WriteEndArray();
            writer.WriteEndMap();
            return writer.Encode();
        }

        private static ATCid Cid(byte[] data) => ATCid.FromSha256Hash(SHA256.HashData(data));

        private static void WriteBlock(Stream stream, ATCid cid, byte[] data)
        {
            byte[] cidBytes = cid.ToBytes();
            WriteVarint(stream, checked((ulong)(cidBytes.Length + data.Length)));
            stream.Write(cidBytes);
            stream.Write(data);
        }

        private static void WriteSection(Stream stream, byte[] data)
        {
            WriteVarint(stream, checked((ulong)data.Length));
            stream.Write(data);
        }

        private static void WriteVarint(Stream stream, ulong value)
        {
            while (value >= 0x80)
            {
                stream.WriteByte((byte)(value | 0x80));
                value >>= 7;
            }

            stream.WriteByte((byte)value);
        }
    }
}
