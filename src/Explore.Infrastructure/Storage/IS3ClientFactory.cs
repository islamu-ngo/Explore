// ABOUTME: Internal factory abstraction for creating configured S3 SDK clients.
// ABOUTME: Allows S3 provider tests to substitute clients without reaching external storage.

using Amazon.S3;
using Explore.Application.Models;

namespace Explore.Infrastructure.Storage;

public interface IS3ClientFactory
{
    IAmazonS3 CreateDataClient(S3Configuration config);

    IAmazonS3 CreatePresignClient(S3Configuration config);
}
