using System;
using System.Collections.Generic;
using System.Text;
using Amazon.S3;
using Amazon.S3.Model;
using Explore.Application.Contracts.Infrastructure;

namespace Explore.Infrastructure.Services
{
    public class ObjectStorageService : IObjectStorageService
    {
        private readonly IAmazonS3 _s3Client;
        private readonly S3Settings _s3Settings;

        public ObjectStorageService(IAmazonS3 s3Client, S3Settings s3Settings)
        {
            _s3Client = s3Client;
            _s3Settings = s3Settings;
        }

        public Task<string> GeneratePresignedUploadUrl(string fileName, string contentType)
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _s3Settings.BucketName,
                Key = fileName,
                Verb = HttpVerb.PUT,
                Expires = DateTime.UtcNow.AddMinutes(40), //DEVELOPMENT MODE! CHANGE IN PRODUCTION ENV
                ContentType = contentType
            };

            string url = _s3Client.GetPreSignedURL(request);
            return Task.FromResult(url);
        }

    }
}
