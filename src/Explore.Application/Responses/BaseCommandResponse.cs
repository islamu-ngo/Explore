using System;
using System.Collections.Generic;
using System.Text;

namespace Explore.Application.Responses;

public class BaseCommandResponse<TKey>
{
    public TKey? Id { get; set; }
    public bool Success { get; set; }
    public string? Message { get; set; }
    public List<string>? Errors { get; set; }
    /// <summary>
    /// Machine-readable failure code for structured error handling by API consumers and UI.
    /// Null on success or when no specific failure code applies.
    /// </summary>
    public string? FailureCode { get; set; }

    /// <summary>
    /// Structured quota metadata for failures that should surface as quota_exceeded ProblemDetails at API boundaries.
    /// </summary>
    public QuotaExceededDetails? QuotaExceeded { get; set; }

    public void SetQuotaExceeded(
        string message,
        QuotaExceededDetails quotaExceeded,
        string? error = null)
    {
        Success = false;
        Message = message;
        FailureCode = FailureCodes.QuotaExceeded;
        QuotaExceeded = quotaExceeded;
        Errors = [error ?? quotaExceeded.ToErrorMessage()];
    }
}
