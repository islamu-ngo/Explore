// ABOUTME: Unit tests for legacy presigned upload URL command validation.
// ABOUTME: Ensures unsafe browser input is rejected before invoking the object storage service.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.DTOs.StorageObject;
using Explore.Application.Features.StorageObjects.Handlers.Commands;
using Explore.Application.Features.StorageObjects.Requests.Commands;
using FluentValidation;
using NSubstitute;

namespace Event.Application.UnitTests.Features.StorageObjects.Commands;

public sealed class GenerateUploadUrlCommandHandlerTests
{
    private readonly IObjectStorageService _objectStorageService = Substitute.For<IObjectStorageService>();

    [Test]
    public async Task Handle_WithValidRequest_NormalizesAndCallsStorageService()
    {
        var expected = new UploadUrlResponseDto
        {
            UploadUrl = "https://storage.example/upload",
            ObjectKey = "uploads/report.pdf",
            ViewUrl = "uploads/report.pdf",
            ExpiresInMinutes = 15
        };
        _objectStorageService.GeneratePresignedUploadUrl("report.pdf", "application/pdf")
            .Returns(expected);

        var result = await CreateHandler().Handle(
            new GenerateUploadUrlCommand
            {
                FileName = " report.pdf ",
                ContentType = " application/pdf "
            },
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(expected);
        await _objectStorageService.Received(1)
            .GeneratePresignedUploadUrl("report.pdf", "application/pdf");
    }

    [Test]
    public async Task Handle_WithPathStyleFileName_ThrowsValidationExceptionWithoutCallingStorage()
    {
        var exception = await Assert.ThrowsAsync<ValidationException>(async () =>
            await CreateHandler().Handle(
                new GenerateUploadUrlCommand
                {
                    FileName = "../secret.pdf",
                    ContentType = "application/pdf"
                },
                CancellationToken.None));

        await Assert.That(exception.Errors.Any(error => error.PropertyName == nameof(UploadRequestDto.FileName))).IsTrue();
        await _objectStorageService.DidNotReceiveWithAnyArgs()
            .GeneratePresignedUploadUrl(default!, default!);
    }

    [Test]
    public async Task Handle_WithWildcardContentType_ThrowsValidationExceptionWithoutCallingStorage()
    {
        var exception = await Assert.ThrowsAsync<ValidationException>(async () =>
            await CreateHandler().Handle(
                new GenerateUploadUrlCommand
                {
                    FileName = "report.pdf",
                    ContentType = "application/*"
                },
                CancellationToken.None));

        await Assert.That(exception.Errors.Any(error => error.PropertyName == nameof(UploadRequestDto.ContentType))).IsTrue();
        await _objectStorageService.DidNotReceiveWithAnyArgs()
            .GeneratePresignedUploadUrl(default!, default!);
    }

    private GenerateUploadUrlCommandHandler CreateHandler()
        => new(_objectStorageService);
}
