using System;
using System.Collections.Generic;
using System.Text;

namespace Explore.Application.DTOs.StorageObject;

public class UploadRequestDto
{
    public required string FileName { get; set; }
    public required string ContentType { get; set; }
}
