using CommerceHub.ProductCatalog.Application.Categories;
using CommerceHub.ProductCatalog.Application.Products;
using CommerceHub.ProductCatalog.Application.Products.Commands;
using CommerceHub.ProductCatalog.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace CommerceHub.ProductCatalog.UnitTests.Products;

public sealed class AssignProductCategoryCommandHandlerTests : IDisposable
{
    private readonly TestDb _db = new();
    private readonly AssignProductCategoryCommandHandler _handler;

    public AssignProductCategoryCommandHandlerTests() => _handler = new AssignProductCategoryCommandHandler(_db);

    [Fact]
    public async Task Missing_product_returns_not_found()
    {
        var category = await _db.SeedCategoryAsync("Men", "men");

        var result = await _handler.HandleAsync(new AssignProductCategoryCommand(Guid.NewGuid(), category.Id));

        result.ShouldFailWith(ProductErrors.NotFound);
    }

    [Fact]
    public async Task Missing_category_returns_not_found()
    {
        var product = await _db.SeedProductAsync();

        var result = await _handler.HandleAsync(new AssignProductCategoryCommand(product.Id, 999));

        result.ShouldFailWith(CategoryErrors.NotFound);
        (await _db.FindProductAsync(product.Id))!.Categories.Should().BeEmpty();
    }

    [Fact]
    public async Task Assigning_twice_is_idempotent()
    {
        var category = await _db.SeedCategoryAsync("Men", "men");
        var product = await _db.SeedProductAsync();

        (await _handler.HandleAsync(new AssignProductCategoryCommand(product.Id, category.Id))).ShouldSucceed();
        (await _handler.HandleAsync(new AssignProductCategoryCommand(product.Id, category.Id))).ShouldSucceed();

        (await _db.FindProductAsync(product.Id))!.Categories.Should().ContainSingle(c => c.CategoryId == category.Id);
    }

    [Fact]
    public async Task Concurrent_duplicate_insert_is_treated_as_success()
    {
        var category = await _db.SeedCategoryAsync("Men", "men");
        var product = await _db.SeedProductAsync();
        _db.FailNextSaveWith = TestDb.UniqueViolation("pk_product_categories");

        var result = await _handler.HandleAsync(new AssignProductCategoryCommand(product.Id, category.Id));

        result.ShouldSucceed();
    }

    public void Dispose() => _db.Dispose();
}

public sealed class RemoveProductCategoryCommandHandlerTests : IDisposable
{
    private readonly TestDb _db = new();
    private readonly RemoveProductCategoryCommandHandler _handler;

    public RemoveProductCategoryCommandHandlerTests() => _handler = new RemoveProductCategoryCommandHandler(_db);

    [Fact]
    public async Task Missing_product_returns_not_found()
    {
        var result = await _handler.HandleAsync(new RemoveProductCategoryCommand(Guid.NewGuid(), 1));

        result.ShouldFailWith(ProductErrors.NotFound);
    }

    [Fact]
    public async Task Removes_mapping_only_and_keeps_category()
    {
        var category = await _db.SeedCategoryAsync("Men", "men");
        var product = await _db.SeedProductAsync(categoryIds: [category.Id]);

        var result = await _handler.HandleAsync(new RemoveProductCategoryCommand(product.Id, category.Id));

        result.ShouldSucceed();
        (await _db.FindProductAsync(product.Id))!.Categories.Should().BeEmpty();
        (await _db.NewContext().Categories.AnyAsync(c => c.Id == category.Id)).Should().BeTrue();
    }

    [Fact]
    public async Task Removing_unassigned_category_succeeds()
    {
        var product = await _db.SeedProductAsync();

        var result = await _handler.HandleAsync(new RemoveProductCategoryCommand(product.Id, 999));

        result.ShouldSucceed();
    }

    public void Dispose() => _db.Dispose();
}

public sealed class ReplaceProductCategoriesCommandHandlerTests : IDisposable
{
    private readonly TestDb _db = new();
    private readonly ReplaceProductCategoriesCommandHandler _handler;

    public ReplaceProductCategoriesCommandHandlerTests() => _handler = new ReplaceProductCategoriesCommandHandler(_db);

    [Fact]
    public async Task Null_category_ids_returns_validation_error()
    {
        var product = await _db.SeedProductAsync();

        var result = await _handler.HandleAsync(new ReplaceProductCategoriesCommand(product.Id, null));

        result.ShouldFailWithValidation("'category_ids' is required");
    }

    [Fact]
    public async Task Missing_product_returns_not_found()
    {
        var result = await _handler.HandleAsync(new ReplaceProductCategoriesCommand(Guid.NewGuid(), []));

        result.ShouldFailWith(ProductErrors.NotFound);
    }

    [Fact]
    public async Task Any_invalid_category_leaves_mappings_unchanged()
    {
        var men = await _db.SeedCategoryAsync("Men", "men");
        var shirts = await _db.SeedCategoryAsync("Shirts", "shirts");
        var product = await _db.SeedProductAsync(categoryIds: [men.Id]);

        var result = await _handler.HandleAsync(new ReplaceProductCategoriesCommand(product.Id, [shirts.Id, 999]));

        result.ShouldFailWith(ProductErrors.CategoryNotFound);
        (await _db.FindProductAsync(product.Id))!.Categories.Select(c => c.CategoryId).Should().Equal(men.Id);
    }

    [Fact]
    public async Task Removes_obsolete_and_adds_missing_mappings()
    {
        var men = await _db.SeedCategoryAsync("Men", "men");
        var shirts = await _db.SeedCategoryAsync("Shirts", "shirts");
        var sale = await _db.SeedCategoryAsync("Sale", "sale");
        var product = await _db.SeedProductAsync(categoryIds: [men.Id, shirts.Id]);

        var result = await _handler.HandleAsync(
            new ReplaceProductCategoriesCommand(product.Id, [shirts.Id, sale.Id, sale.Id]));

        result.ShouldSucceed();
        result.Value.Select(c => c.Id).Should().BeEquivalentTo([shirts.Id, sale.Id]);
        (await _db.FindProductAsync(product.Id))!.Categories.Select(c => c.CategoryId)
            .Should().BeEquivalentTo([shirts.Id, sale.Id]);
    }

    [Fact]
    public async Task Empty_list_clears_all_mappings()
    {
        var men = await _db.SeedCategoryAsync("Men", "men");
        var product = await _db.SeedProductAsync(categoryIds: [men.Id]);

        var result = await _handler.HandleAsync(new ReplaceProductCategoriesCommand(product.Id, []));

        result.Value.Should().BeEmpty();
        (await _db.FindProductAsync(product.Id))!.Categories.Should().BeEmpty();
    }

    public void Dispose() => _db.Dispose();
}
