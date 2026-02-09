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
}
