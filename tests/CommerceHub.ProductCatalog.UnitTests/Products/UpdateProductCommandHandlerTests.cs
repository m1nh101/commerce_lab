using CommerceHub.ProductCatalog.Application.Products;
using CommerceHub.ProductCatalog.Application.Products.Commands;
using CommerceHub.ProductCatalog.Domain.Products;
using CommerceHub.ProductCatalog.UnitTests.TestSupport;

namespace CommerceHub.ProductCatalog.UnitTests.Products;

public sealed class UpdateProductCommandHandlerTests : IDisposable
{
    private readonly TestDb _db = new();
    private readonly UpdateProductCommandHandler _handler;

    public UpdateProductCommandHandlerTests() => _handler = new UpdateProductCommandHandler(_db);

    [Fact]
    public async Task Missing_product_returns_not_found()
    {
        var result = await _handler.HandleAsync(
            new UpdateProductCommand(Guid.NewGuid(), new ProductFields("Name", "slug", null, null)));

        result.ShouldFailWith(ProductErrors.NotFound);
    }

    [Fact]
    public async Task Replaces_all_fields_and_keeps_id()
    {
        var product = await _db.SeedProductAsync(status: ProductStatus.Active);

        var result = await _handler.HandleAsync(
            new UpdateProductCommand(product.Id, new ProductFields("New name", "new-slug", null, ProductStatus.Inactive)));

        result.ShouldSucceed();
        result.Value.Id.Should().Be(product.Id);
        var stored = await _db.FindProductAsync(product.Id);
        stored!.Name.Should().Be("New name");
        stored.Slug.Should().Be("new-slug");
        stored.Description.Should().BeNull();
        stored.Status.Should().Be(ProductStatus.Inactive);
    }

    [Fact]
    public async Task Omitted_status_resets_to_draft()
    {
        var product = await _db.SeedProductAsync(status: ProductStatus.Active);

        var result = await _handler.HandleAsync(
            new UpdateProductCommand(product.Id, new ProductFields("Name", product.Slug, null, null)));

        result.Value.Status.Should().Be(ProductStatus.Draft);
    }

    [Fact]
    public async Task Keeping_own_slug_is_not_a_conflict()
    {
        var product = await _db.SeedProductAsync(slug: "same-slug");

        var result = await _handler.HandleAsync(
            new UpdateProductCommand(product.Id, new ProductFields("Renamed", "same-slug", null, null)));

        result.ShouldSucceed();
    }

    [Fact]
    public async Task Changing_slug_to_existing_one_returns_conflict()
    {
        await _db.SeedProductAsync(slug: "taken");
        var product = await _db.SeedProductAsync(slug: "mine");

        var result = await _handler.HandleAsync(
            new UpdateProductCommand(product.Id, new ProductFields("Name", "taken", null, null)));

        result.ShouldFailWith(ProductErrors.SlugConflict);
        (await _db.FindProductAsync(product.Id))!.Slug.Should().Be("mine");
    }

    [Fact]
    public async Task Invalid_fields_return_validation_error()
    {
        var product = await _db.SeedProductAsync();

        var result = await _handler.HandleAsync(
            new UpdateProductCommand(product.Id, new ProductFields(new string('a', 256), "slug", null, null)));

        result.ShouldFailWithValidation("'name' must not exceed 255");
    }

    [Fact]
    public async Task Save_time_slug_unique_violation_maps_to_conflict()
    {
        var product = await _db.SeedProductAsync(slug: "mine");
        _db.FailNextSaveWith = TestDb.UniqueViolation(ProductConstraints.SlugUniqueIndex);

        var result = await _handler.HandleAsync(
            new UpdateProductCommand(product.Id, new ProductFields("Name", "raced", null, null)));

        result.ShouldFailWith(ProductErrors.SlugConflict);
    }

    public void Dispose() => _db.Dispose();
}
