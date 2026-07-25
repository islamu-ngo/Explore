// ABOUTME: Describes optional ATProto thumbnail blob metadata validated before remote acquisition.
// ABOUTME: Keeps source identity and content binding data in the Application layer.

namespace Explore.Application.Features.Federation.Atproto.Models;

public sealed record AtprotoThumbnailBlobCandidate(
    string Did,
    string Cid,
    string MimeType,
    long Size);
