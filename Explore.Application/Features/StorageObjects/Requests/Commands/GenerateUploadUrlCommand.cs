using System;
using System.Collections.Generic;
using System.Text;
using MediatR;

namespace Explore.Application.Features.StorageObjects.Requests.Commands
{
    public class GenerateUploadUrlCommand : IRequest<string>
    {
        public string FileName { get; set; }
        public string ContentType { get; set; }
    }
}
