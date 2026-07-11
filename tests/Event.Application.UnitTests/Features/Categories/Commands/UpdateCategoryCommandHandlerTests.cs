// ABOUTME: Unit tests for grouped category update command handling.
// ABOUTME: Covers validation, optimistic concurrency, explicit field updates, OptionalUpdate clear semantics, and cache invalidation.

using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Category;
using Explore.Application.Exceptions;
using Explore.Application.Features.Categories.Handlers.Commands;
using Explore.Application.Features.Categories.Requests.Commands;
using Explore.Application.Models.Common;
using Explore.Domain;
using Microsoft.Extensions.Caching.Hybrid;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.Categories.Commands;

public class UpdateCategoryCommandHandlerTests
{
    private readonly ICategoryRepository _categoryRepository = Substitute.For<ICategoryRepository>();
    private readonly HybridCache _cache = Substitute.For<HybridCache>();
    private readonly UpdateCategoryCommandHandler _handler;

    public UpdateCategoryCommandHandlerTests()
    {
        _handler = new UpdateCategoryCommandHandler(_categoryRepository, _cache);
    }

    [Test]
    public async Task Handle_WhenWrapperHasNoGroups_ReturnsValidationFailureAndDoesNotSave()
    {
        var result = await _handler.Handle(new UpdateCategoryCommand
        {
            CategoryId = Guid.CreateVersion7(),
            ExpectedConcurrencyStamp = Guid.CreateVersion7(),
            UpdateCategoryDto = new UpdateCategoryDto()
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).IsEqualTo("Category update failed.");
        await _categoryRepository.DidNotReceive().Update(Arg.Any<Category>());
        await _cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenExpectedConcurrencyStampIsStale_ThrowsConflictAndDoesNotSave()
    {
        var category = CreateCategory();
        _categoryRepository.GetById(category.Id).Returns(category);

        await Assert.That(async () => await _handler.Handle(new UpdateCategoryCommand
        {
            CategoryId = category.Id,
            ExpectedConcurrencyStamp = Guid.CreateVersion7(),
            UpdateCategoryDto = new UpdateCategoryDto
            {
                FullName = new UpdateCategoryFullNameDto { Value = "Updated Category" }
            }
        }, CancellationToken.None)).Throws<ConcurrencyConflictException>();

        await _categoryRepository.DidNotReceive().Update(Arg.Any<Category>());
        await _cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenSingleFieldGroupIsPresent_UpdatesOnlyThatField()
    {
        var category = CreateCategory(parentId: Guid.CreateVersion7());
        _categoryRepository.GetById(category.Id).Returns(category);

        var result = await _handler.Handle(new UpdateCategoryCommand
        {
            CategoryId = category.Id,
            ExpectedConcurrencyStamp = category.ConcurrencyStamp,
            UpdateCategoryDto = new UpdateCategoryDto
            {
                FullName = new UpdateCategoryFullNameDto { Value = "Updated Category" }
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(category.FullName).IsEqualTo("Updated Category");
        await Assert.That(category.MasterCode).IsEqualTo("EXISTING");
        await Assert.That(category.ParentId).IsNotNull();
        await _categoryRepository.Received(1).Update(category);
        await _cache.Received(1).RemoveAsync("categories:list:1:20", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenParentUpdateExplicitlyClearsParent_SetsParentIdToNull()
    {
        var category = CreateCategory(parentId: Guid.CreateVersion7());
        _categoryRepository.GetById(category.Id).Returns(category);

        var result = await _handler.Handle(new UpdateCategoryCommand
        {
            CategoryId = category.Id,
            ExpectedConcurrencyStamp = category.ConcurrencyStamp,
            UpdateCategoryDto = new UpdateCategoryDto
            {
                Parent = new UpdateCategoryParentDto
                {
                    ParentId = OptionalUpdate<Guid?>.Set(null)
                }
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(category.ParentId).IsNull();
        await _categoryRepository.Received(1).Update(category);
    }

    [Test]
    public async Task Handle_WhenParentGroupHasNoFieldOperation_ReturnsValidationFailure()
    {
        var result = await _handler.Handle(new UpdateCategoryCommand
        {
            CategoryId = Guid.CreateVersion7(),
            ExpectedConcurrencyStamp = Guid.CreateVersion7(),
            UpdateCategoryDto = new UpdateCategoryDto
            {
                Parent = new UpdateCategoryParentDto()
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Errors).Contains("Parent group must include ParentId.");
        await _categoryRepository.DidNotReceive().Update(Arg.Any<Category>());
    }

    private static Category CreateCategory(Guid? parentId = null)
    {
        return new Category
        {
            Id = Guid.CreateVersion7(),
            ConcurrencyStamp = Guid.CreateVersion7(),
            MasterCode = "EXISTING",
            FullName = "Existing Category",
            ParentId = parentId,
            TenantId = Guid.CreateVersion7(),
            Tenant = null!
        };
    }
}
