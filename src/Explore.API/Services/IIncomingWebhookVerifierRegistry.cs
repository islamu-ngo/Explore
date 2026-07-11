// ABOUTME: API composition contract for resolving provider-specific incoming webhook verifiers.
// ABOUTME: Keeps controllers provider-neutral while preserving strict per-provider verification logic.

using Explore.Application.Contracts.Webhooks;

namespace Explore.API.Services;

public interface IIncomingWebhookVerifierRegistry
{
    IIncomingWebhookVerifier GetRequired(string provider);
}
