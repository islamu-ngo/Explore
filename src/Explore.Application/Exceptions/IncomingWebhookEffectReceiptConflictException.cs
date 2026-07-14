// ABOUTME: Signals a concurrent insert of the unique incoming-webhook effect receipt identity.
// ABOUTME: Lets Application recover a matching committed effect without depending on PostgreSQL exception types.

namespace Explore.Application.Exceptions;

public sealed class IncomingWebhookEffectReceiptConflictException(Exception innerException)
    : ApplicationException(
        "A concurrent processor committed the incoming webhook effect receipt.",
        innerException);
