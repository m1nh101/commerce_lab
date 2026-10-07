using CommerceHub.ProductCatalog.Application.Products;
using CommerceHub.ProductCatalog.Application.Products.Commands;
using CommerceHub.ProductCatalog.Domain.Products;
using CommerceHub.ProductCatalog.UnitTests.TestSupport;

namespace CommerceHub.ProductCatalog.UnitTests.Products;

public sealed class ChangeProductStatusCommandHandlerTests : IDisposable
{
    private readonly TestDb _db = new();
    private readonly ChangeProductStatusCommandHandler _handler;

    public ChangeProductStatusCommandHandlerTests() => _handler = new ChangeProductStatusCommandHandler(_db);

    [Fact]
    public async Task Missing_status_returns_validation_error()
    {
        var product = await _db.SeedProductAsync();

        var result = await _handler.HandleAsync(new ChangeProductStatusCommand(product.Id, null));

        result.ShouldFailWithValidation("'status' is required");
    }

    [Fact]
    public async Task Undefined_status_returns_validation_error()
    {
        var product = await _db.SeedProductAsync();

        var result = await _handler.HandleAsync(new ChangeProductStatusCommand(product.Id, (ProductStatus)42));

        result.ShouldFailWith(ProductValidator.InvalidStatus);
    }

    [Fact]
    public async Task Missing_product_returns_not_found()
    {
        var result = await _handler.HandleAsync(new ChangeProductStatusCommand(Guid.NewGuid(), ProductStatus.Active));

        result.ShouldFailWith(ProductErrors.NotFound);
    }

    // No transition matrix is enforced yet (spec §4), so any defined status is reachable from any other.
    [Theory]
    [InlineData(ProductStatus.Draft, ProductStatus.Active)]
    [InlineData(ProductStatus.Active, ProductStatus.Inactive)]
    [InlineData(ProductStatus.Inactive, ProductStatus.Archived)]
    [InlineData(ProductStatus.Archived, ProductStatus.Draft)]
    public async Task Changes_to_any_defined_status(ProductStatus from, ProductStatus to)
    {
        var product = await _db.SeedProductAsync(status: from);

        var result = await _handler.HandleAsync(new ChangeProductStatusCommand(product.Id, to));

        result.Value.Status.Should().Be(to);
        (await _db.FindProductAsync(product.Id))!.Status.Should().Be(to);
    }

    public void Dispose() => _db.Dispose();
}
