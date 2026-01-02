using System;
using System.Collections.Generic;
using System.Text;

namespace Explore.Application.Contracts.Infrastructure
{
    public interface IObjectStorageService
    {
        Task<string> GeneratePresignedUploadUrl(string fileName, string contentType);
    }
}
