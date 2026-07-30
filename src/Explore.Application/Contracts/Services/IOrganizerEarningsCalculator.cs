// ABOUTME: Defines the application contract for exact organizer earnings from platform-fee policy snapshots.
// ABOUTME: Accepts and returns integer minor units so checkout never relies on floating-point money calculations.

using Explore.Domain;

namespace Explore.Application.Contracts.Services;

public interface IOrganizerEarningsCalculator
{
    OrganizerEarnings Calculate(string currencyCode, long organizerDirectedTotalMinor, PlatformFeePolicy? platformFeePolicy);
}

public sealed record OrganizerEarnings(
    long OrganizerDirectedTotalMinor,
    long PlatformFeeMinor,
    long OrganizerEarningsMinor,
    int? PlatformFeePolicyVersionSnapshot);
