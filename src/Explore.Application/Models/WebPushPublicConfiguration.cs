// ABOUTME: Browser-safe Web Push configuration returned to downstream API/BFF lanes.
// ABOUTME: Contains only the VAPID public key and enabled flag, never server private key material.

namespace Explore.Application.Models;

public sealed record WebPushPublicConfiguration(bool Enabled, string PublicKey);
