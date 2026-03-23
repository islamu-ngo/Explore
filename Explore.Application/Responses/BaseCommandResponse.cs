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
}
