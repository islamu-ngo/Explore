// ABOUTME: Focused tests for grouped TagTypeTags and CategoryTypeCategories relationship updates.
// ABOUTME: Verifies sparse updates, duplicate rejection, and persisted tenant isolation.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.CategoryTypeCategories;
using Explore.Application.DTOs.TagTypeTags;
using Explore.Application.Features.CategoryTypeCategories.Handlers.Commands;
using Explore.Application.Features.CategoryTypeCategories.Requests.Commands;
using Explore.Application.Features.TagTypeTags.Handlers.Commands;
using Explore.Application.Features.TagTypeTags.Requests.Commands;
using Explore.Domain;
using NSubstitute;

namespace Event.Application.UnitTests.Features.LookupRelationships;

public sealed class UpdateLookupRelationshipCommandHandlerTests
{
    [Test]
    public async Task TagTypeUpdate_MergesSparseRelationshipAndSavesOnce()
    {
        Guid tenantId = Guid.NewGuid();
        Guid linkId = Guid.NewGuid();
        Guid tagId = Guid.NewGuid();
        var link = CreateTagLink(linkId, tenantId, tagId, 1);
        var repository = Substitute.For<ITagTypeTagsRepository>();
        var tagRepository = Substitute.For<ITagRepository>();
        var typeRepository = Substitute.For<ITagTypeRepository>();
        repository.GetById(linkId).Returns(link);
        tagRepository.GetById(tagId).Returns(link.Tag);
        typeRepository.Exists(2).Returns(true);
        repository.Exists(tagId, 2).Returns(false);

        var handler = new UpdateTagTypeTagsCommandHandler(
            repository,
            tagRepository,
            typeRepository,
            CreateTenantContext(tenantId));
        var result = await handler.Handle(new UpdateTagTypeTagsCommand
        {
            TagTypeTagsId = linkId,
            TagTypeTagsDto = new UpdateTagTypeTagsDto
            {
                Relationship = new UpdateTagTypeTagsRelationshipDto { TagTypeId = 2 }
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(link.TagId).IsEqualTo(tagId);
        await Assert.That(link.TagTypeId).IsEqualTo(2);
        await repository.Received(1).Update(link);
    }

    [Test]
    public async Task TagTypeUpdate_RejectsCrossTenantTag()
    {
        Guid tenantId = Guid.NewGuid();
        Guid linkId = Guid.NewGuid();
        Guid targetTagId = Guid.NewGuid();
        var link = CreateTagLink(linkId, tenantId, Guid.NewGuid(), 1);
        var repository = Substitute.For<ITagTypeTagsRepository>();
        var tagRepository = Substitute.For<ITagRepository>();
        var typeRepository = Substitute.For<ITagTypeRepository>();
        repository.GetById(linkId).Returns(link);
        tagRepository.GetById(targetTagId).Returns(CreateTag(targetTagId, Guid.NewGuid()));
        typeRepository.Exists(1).Returns(true);

        var handler = new UpdateTagTypeTagsCommandHandler(
            repository,
            tagRepository,
            typeRepository,
            CreateTenantContext(tenantId));
        var result = await handler.Handle(new UpdateTagTypeTagsCommand
        {
            TagTypeTagsId = linkId,
            TagTypeTagsDto = new UpdateTagTypeTagsDto
            {
                Relationship = new UpdateTagTypeTagsRelationshipDto { TagId = targetTagId }
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await repository.DidNotReceive().Update(Arg.Any<TagTypeTags>());
    }

    [Test]
    public async Task CategoryTypeUpdate_RejectsChangedDuplicatePair()
    {
        Guid tenantId = Guid.NewGuid();
        Guid linkId = Guid.NewGuid();
        Guid categoryId = Guid.NewGuid();
        var link = CreateCategoryLink(linkId, tenantId, categoryId, 1);
        var repository = Substitute.For<ICategoryTypeCategoriesRepository>();
        var categoryRepository = Substitute.For<ICategoryRepository>();
        var typeRepository = Substitute.For<ICategoryTypeRepository>();
        repository.GetById(linkId).Returns(link);
        categoryRepository.GetById(categoryId).Returns(link.Category);
        typeRepository.Exists(2).Returns(true);
        repository.Exists(categoryId, 2).Returns(true);

        var handler = new UpdateCategoryTypeCategoriesCommandHandler(
            repository,
            categoryRepository,
            typeRepository,
            CreateTenantContext(tenantId));
        var result = await handler.Handle(new UpdateCategoryTypeCategoriesCommand
        {
            CategoryTypeCategoriesId = linkId,
            CategoryTypeCategoriesDto = new UpdateCategoryTypeCategoriesDto
            {
                Relationship = new UpdateCategoryTypeCategoriesRelationshipDto { CategoryTypeId = 2 }
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Errors).Contains("Category and Category Type relationship already exists.");
        await repository.DidNotReceive().Update(Arg.Any<CategoryTypeCategories>());
    }

    [Test]
    public async Task CategoryTypeUpdate_PreservesUnspecifiedType()
    {
        Guid tenantId = Guid.NewGuid();
        Guid linkId = Guid.NewGuid();
        Guid targetCategoryId = Guid.NewGuid();
        var link = CreateCategoryLink(linkId, tenantId, Guid.NewGuid(), 3);
        var targetCategory = CreateCategory(targetCategoryId, tenantId);
        var repository = Substitute.For<ICategoryTypeCategoriesRepository>();
        var categoryRepository = Substitute.For<ICategoryRepository>();
        var typeRepository = Substitute.For<ICategoryTypeRepository>();
        repository.GetById(linkId).Returns(link);
        categoryRepository.GetById(targetCategoryId).Returns(targetCategory);
        typeRepository.Exists(3).Returns(true);
        repository.Exists(targetCategoryId, 3).Returns(false);

        var handler = new UpdateCategoryTypeCategoriesCommandHandler(
            repository,
            categoryRepository,
            typeRepository,
            CreateTenantContext(tenantId));
        var result = await handler.Handle(new UpdateCategoryTypeCategoriesCommand
        {
            CategoryTypeCategoriesId = linkId,
            CategoryTypeCategoriesDto = new UpdateCategoryTypeCategoriesDto
            {
                Relationship = new UpdateCategoryTypeCategoriesRelationshipDto { CategoryId = targetCategoryId }
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(link.CategoryId).IsEqualTo(targetCategoryId);
        await Assert.That(link.CategoryTypeId).IsEqualTo(3);
        await repository.Received(1).Update(link);
    }

    private static ITenantContext CreateTenantContext(Guid tenantId)
    {
        var context = Substitute.For<ITenantContext>();
        context.TenantId.Returns(tenantId);
        return context;
    }

    private static TagTypeTags CreateTagLink(Guid id, Guid tenantId, Guid tagId, int typeId) => new()
    {
        Id = id,
        TenantId = tenantId,
        Tenant = null!,
        TagId = tagId,
        Tag = CreateTag(tagId, tenantId),
        TagTypeId = typeId,
        TagType = new TagType { Id = typeId, FullName = "Type", MasterCode = "TYPE" }
    };

    private static CategoryTypeCategories CreateCategoryLink(Guid id, Guid tenantId, Guid categoryId, int typeId) => new()
    {
        Id = id,
        TenantId = tenantId,
        Tenant = null!,
        CategoryId = categoryId,
        Category = CreateCategory(categoryId, tenantId),
        CategoryTypeId = typeId,
        CategoryType = new CategoryType { Id = typeId, FullName = "Type", MasterCode = "TYPE" }
    };

    private static Tag CreateTag(Guid id, Guid tenantId) => new()
    {
        Id = id,
        TenantId = tenantId,
        Tenant = null!,
        FullName = "Tag",
        MasterCode = "TAG"
    };

    private static Category CreateCategory(Guid id, Guid tenantId) => new()
    {
        Id = id,
        TenantId = tenantId,
        Tenant = null!,
        FullName = "Category",
        MasterCode = "CATEGORY"
    };
}
