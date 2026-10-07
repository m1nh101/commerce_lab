using CommerceHub.ProductCatalog.Application.Products;
using CommerceHub.ProductCatalog.Application.Products.Commands;
using CommerceHub.ProductCatalog.Domain.Products;
using CommerceHub.ProductCatalog.UnitTests.TestSupport;

namespace CommerceHub.ProductCatalog.UnitTests.Products;

public sealed class PatchProductCommandHandlerTests : IDisposable
{
    private readonly TestDb _db = new();
    private readonly PatchProductCommandHandler _handler;

    public PatchProductCommandHandlerTests() => _handler = new PatchProductCommandHandler(_db);

    private static ProductPatch Patch(
        string? name = null,
        string? slug = null,
        ProductStatus? status = null,
        bool descriptionSpecified = false,
        string? description = null) => new(name, slug, status, descriptionSpecified, description);

    [Fact]
    public async Task Missing_product_returns_not_found()
    {
        var result = await _handler.HandleAsync(new PatchProductCommand(Guid.NewGuid(), Patch(name: "x")));

        result.ShouldFailWith(ProductErrors.NotFound);
    }

    [Fact]
    public async Task Omitted_fields_remain_unchanged()
    {
        var product = await _db.SeedProductAsync("Shirt", "shirt", "Cotton", ProductStatus.Active);

        var result = await _handler.HandleAsync(new PatchProductCommand(product.Id, Patch(name: "Polo")));

        result.ShouldSucceed();
        result.Value.Should().BeEquivalentTo(
            new { Name = "Polo", Slug = "shirt", Description = "Cotton", Status = ProductStatus.Active });
    }

    [Fact]
    public async Task Explicit_null_description_clears_it()
    {
        var product = await _db.SeedProductAsync(description: "Cotton");

        var result = await _handler.HandleAsync(
            new PatchProductCommand(product.Id, Patch(descriptionSpecified: true, description: null)));

        result.Value.Description.Should().BeNull();
        (await _db.FindProductAsync(product.Id))!.Description.Should().BeNull();
    }

    [Fact]
    public async Task Status_only_patch_changes_status()
    {
        var product = await _db.SeedProductAsync();

        var result = await _handler.HandleAsync(new PatchProductCommand(product.Id, Patch(status: ProductStatus.Archived)));

        result.Value.Status.Should().Be(ProductStatus.Archived);
    }

    [Fact]
    public async Task Invalid_merged_result_returns_validation_error()
    {
        var product = await _db.SeedProductAsync();

        var result = await _handler.HandleAsync(new PatchProductCommand(product.Id, Patch(slug: "Not A Slug")));

        result.ShouldFailWithValidation("'slug' must be lowercase");
    }

    [Fact]
    public async Task Slug_taken_by_another_product_returns_conflict()
    {
        await _db.SeedProductAsync(slug: "taken");
        var product = await _db.SeedProductAsync(slug: "mine");

        var result = await _handler.HandleAsync(new PatchProductCommand(product.Id, Patch(slug: "taken")));

        result.ShouldFailWith(ProductErrors.SlugConflict);
    }

    public void Dispose() => _db.Dispose();
}
