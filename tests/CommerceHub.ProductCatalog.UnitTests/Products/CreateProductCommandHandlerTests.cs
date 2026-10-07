using CommerceHub.ProductCatalog.Application.Common;
using CommerceHub.ProductCatalog.Application.Products;
using CommerceHub.ProductCatalog.Application.Products.Commands;
using CommerceHub.ProductCatalog.Application.Variants;
using CommerceHub.ProductCatalog.Domain.Products;
using CommerceHub.ProductCatalog.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace CommerceHub.ProductCatalog.UnitTests.Products;

public sealed class CreateProductCommandHandlerTests : IDisposable
{
    private readonly TestDb _db = new();
    private readonly CreateProductCommandHandler _handler;

    public CreateProductCommandHandlerTests() => _handler = new CreateProductCommandHandler(_db);

    private static ProductFields Fields(string slug = "mens-casual-t-shirt", ProductStatus? status = null) =>
        new("Men's Casual T-Shirt", slug, "Cotton casual T-shirt.", status);

    private static VariantFields Variant(string sku) => new(sku, "Size M", 19.99m, "USD", null, null);

    [Fact]
    public async Task Creates_draft_product_with_server_generated_id()
    {
        var result = await _handler.HandleAsync(new CreateProductCommand(Fields()));

        result.ShouldSucceed();
        var dto = result.Value;
        dto.Id.Should().NotBeEmpty();
        dto.Name.Should().Be("Men's Casual T-Shirt");
        dto.Slug.Should().Be("mens-casual-t-shirt");
        dto.Status.Should().Be(ProductStatus.Draft);
        (await _db.FindProductAsync(dto.Id)).Should().NotBeNull();
    }

    [Fact]
    public async Task Uses_requested_status()
    {
        var result = await _handler.HandleAsync(new CreateProductCommand(Fields(status: ProductStatus.Active)));

        result.Value.Status.Should().Be(ProductStatus.Active);
    }

    [Fact]
    public async Task Duplicate_slug_returns_conflict_and_persists_nothing()
    {
        await _db.SeedProductAsync(slug: "mens-casual-t-shirt");

        var result = await _handler.HandleAsync(new CreateProductCommand(Fields()));

        result.ShouldFailWith(ProductErrors.SlugConflict);
        result.Error!.Code.Should().Be("PRODUCT_SLUG_ALREADY_EXISTS");
        result.Error.Type.Should().Be(ErrorType.Conflict);
        (await _db.NewContext().Products.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Invalid_fields_return_validation_error()
    {
        var result = await _handler.HandleAsync(new CreateProductCommand(new ProductFields(null, "slug", null, null)));

        result.ShouldFailWithValidation("'name' is required");
        (await _db.NewContext().Products.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Unknown_category_returns_not_found_and_persists_nothing()
    {
        var category = await _db.SeedCategoryAsync("Men", "men");

        var result = await _handler.HandleAsync(new CreateProductCommand(Fields(), CategoryIds: [category.Id, 999]));

        result.ShouldFailWith(ProductErrors.CategoryNotFound);
        (await _db.NewContext().Products.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Duplicate_skus_in_payload_return_validation_error_and_persist_nothing()
    {
        var result = await _handler.HandleAsync(
            new CreateProductCommand(Fields(), Variants: [Variant("TS-M"), Variant("ts-m")]));

        result.ShouldFailWithValidation("appears more than once");
        (await _db.NewContext().Products.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Assigns_categories_without_duplicates()
    {
        var men = await _db.SeedCategoryAsync("Men", "men");
        var shirts = await _db.SeedCategoryAsync("Shirts", "shirts");

        var result = await _handler.HandleAsync(
            new CreateProductCommand(Fields(), CategoryIds: [men.Id, shirts.Id, men.Id]));

        result.ShouldSucceed();
        result.Value.Categories!.Select(c => c.Id).Should().BeEquivalentTo([men.Id, shirts.Id]);
        (await _db.FindProductAsync(result.Value.Id))!.Categories.Should().HaveCount(2);
    }

    [Fact]
    public async Task Creates_variants_in_same_command()
    {
        var result = await _handler.HandleAsync(
            new CreateProductCommand(Fields(), Variants: [Variant("TS-M"), Variant("TS-L")]));

        result.ShouldSucceed();
        result.Value.Variants!.Select(v => v.Sku).Should().BeEquivalentTo(["TS-L", "TS-M"]);
    }

    [Fact]
    public async Task Save_time_slug_unique_violation_maps_to_conflict()
    {
        _db.FailNextSaveWith = TestDb.UniqueViolation(ProductConstraints.SlugUniqueIndex);

        var result = await _handler.HandleAsync(new CreateProductCommand(Fields()));

        result.ShouldFailWith(ProductErrors.SlugConflict);
    }

    public void Dispose() => _db.Dispose();
}
