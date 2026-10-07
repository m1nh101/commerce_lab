using CommerceHub.ProductCatalog.Application.Products;
using CommerceHub.ProductCatalog.Application.Products.Queries;
using CommerceHub.ProductCatalog.Domain.Products;
using CommerceHub.ProductCatalog.UnitTests.TestSupport;

namespace CommerceHub.ProductCatalog.UnitTests.Products;

public sealed class GetProductQueryHandlerTests : IDisposable
{
    private readonly TestDb _db = new();
    private readonly GetProductQueryHandler _handler;

    public GetProductQueryHandlerTests() => _handler = new GetProductQueryHandler(_db);

    [Fact]
    public async Task Missing_product_returns_not_found()
    {
        var result = await _handler.HandleAsync(new GetProductQuery(Guid.NewGuid()));

        result.ShouldFailWith(ProductErrors.NotFound);
    }

    [Fact]
    public async Task Without_includes_child_sections_are_null()
    {
        var product = await _db.SeedProductAsync();

        var result = await _handler.HandleAsync(new GetProductQuery(product.Id));

        result.Value.Id.Should().Be(product.Id);
        result.Value.Variants.Should().BeNull();
        result.Value.Categories.Should().BeNull();
    }

    [Fact]
    public async Task Includes_assigned_categories_when_requested()
    {
        var men = await _db.SeedCategoryAsync("Men", "men");
        await _db.SeedCategoryAsync("Women", "women");
        var product = await _db.SeedProductAsync(categoryIds: [men.Id]);

        var result = await _handler.HandleAsync(new GetProductQuery(product.Id, ProductIncludes.Categories));

        result.Value.Categories.Should().BeEquivalentTo([new ProductCategoryDto(men.Id, "Men", "men")]);
    }

    // Spec §10: stock is owned by a separate service and must never leak into catalog read models.
    [Theory]
    [InlineData(typeof(ProductDto))]
    [InlineData(typeof(ProductDetailDto))]
    public void Product_read_models_expose_no_stock_data(Type dto) =>
        dto.GetProperties().Select(p => p.Name)
            .Should().NotContain(n => n.Contains("Stock", StringComparison.OrdinalIgnoreCase)
                                      || n.Contains("Quantity", StringComparison.OrdinalIgnoreCase));

    public void Dispose() => _db.Dispose();
}

public sealed class ListProductsQueryHandlerTests : IDisposable
{
    private readonly TestDb _db = new();
    private readonly ListProductsQueryHandler _handler;

    public ListProductsQueryHandlerTests() => _handler = new ListProductsQueryHandler(_db);

    private static ListProductsQuery Query(
        ProductStatus? status = null,
        long? categoryId = null,
        string? search = null,
        string? slug = null,
        int page = 1,
        int limit = 20) => new(status, categoryId, search, slug, ProductSort.NameAsc, page, limit);

    [Theory]
    [InlineData(0, 20, "'page'")]
    [InlineData(1, 0, "'limit'")]
    [InlineData(1, 101, "'limit'")]
    public async Task Invalid_paging_returns_validation_error(int page, int limit, string field)
    {
        var result = await _handler.HandleAsync(Query(page: page, limit: limit));

        result.ShouldFailWithValidation(field);
    }

    [Fact]
    public async Task Page_size_of_100_is_allowed() =>
        (await _handler.HandleAsync(Query(limit: 100))).ShouldSucceed();

    [Fact]
    public async Task Filters_by_status()
    {
        await _db.SeedProductAsync("A", "a", status: ProductStatus.Active);
        await _db.SeedProductAsync("B", "b", status: ProductStatus.Draft);

        var result = await _handler.HandleAsync(Query(status: ProductStatus.Active));

        result.Value.Items.Select(p => p.Slug).Should().Equal("a");
    }

    [Fact]
    public async Task Filters_by_category()
    {
        var men = await _db.SeedCategoryAsync("Men", "men");
        await _db.SeedProductAsync("A", "a", categoryIds: [men.Id]);
        await _db.SeedProductAsync("B", "b");

        var result = await _handler.HandleAsync(Query(categoryId: men.Id));

        result.Value.Items.Select(p => p.Slug).Should().Equal("a");
    }

    [Fact]
    public async Task Search_is_case_insensitive_on_name_or_slug()
    {
        await _db.SeedProductAsync("Casual Shirt", "casual-shirt");
        await _db.SeedProductAsync("Jeans", "denim-shirt-alike");
        await _db.SeedProductAsync("Hat", "hat");

        var result = await _handler.HandleAsync(Query(search: "SHIRT"));

        result.Value.Items.Select(p => p.Slug).Should().BeEquivalentTo(["casual-shirt", "denim-shirt-alike"]);
    }

    [Fact]
    public async Task Slug_is_exact_match()
    {
        await _db.SeedProductAsync("A", "shirt");
        await _db.SeedProductAsync("B", "shirt-xl");

        var result = await _handler.HandleAsync(Query(slug: "shirt"));

        result.Value.Items.Select(p => p.Slug).Should().Equal("shirt");
    }

    [Fact]
    public async Task Paginates_and_reports_total()
    {
        foreach (var n in new[] { "a", "b", "c", "d", "e" })
        {
            await _db.SeedProductAsync(n, n);
        }

        var result = await _handler.HandleAsync(Query(page: 2, limit: 2));

        result.Value.Items.Select(p => p.Slug).Should().Equal("c", "d");
        result.Value.Total.Should().Be(5);
        result.Value.TotalPages.Should().Be(3);
    }

    public void Dispose() => _db.Dispose();
}
