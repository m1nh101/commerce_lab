using CommerceHub.ProductCatalog.Domain.Products;

namespace CommerceHub.ProductCatalog.UnitTests.Domain;

public sealed class ProductTests
{
    [Fact]
    public void Create_generates_uuid_v7_id_and_defaults_to_draft()
    {
        var product = Product.Create("  Shirt  ", " shirt ", "desc");

        product.Id.Version.Should().Be(7);
        product.Status.Should().Be(ProductStatus.Draft);
        product.Name.Should().Be("Shirt");
        product.Slug.Should().Be("shirt");
        product.Description.Should().Be("desc");
    }

    [Theory]
    [InlineData("", "slug")]
    [InlineData("   ", "slug")]
    [InlineData("name", "")]
    [InlineData("name", "  ")]
    public void Create_rejects_empty_name_or_slug(string name, string slug)
    {
        var act = () => Product.Create(name, slug);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_rejects_name_and_slug_longer_than_255()
    {
        var tooLong = new string('a', Product.NameMaxLength + 1);

        FluentActions.Invoking(() => Product.Create(tooLong, "slug")).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => Product.Create("name", tooLong)).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => Product.Create(new string('a', 255), new string('a', 255))).Should().NotThrow();
    }

    [Fact]
    public void UpdateDetails_changes_fields_but_not_id()
    {
        var product = Product.Create("Old", "old");
        var id = product.Id;

        product.UpdateDetails("New", "new", null);

        product.Id.Should().Be(id);
        product.Name.Should().Be("New");
        product.Slug.Should().Be("new");
        product.Description.Should().BeNull();
    }

    [Theory]
    [InlineData(ProductStatus.Draft)]
    [InlineData(ProductStatus.Active)]
    [InlineData(ProductStatus.Inactive)]
    [InlineData(ProductStatus.Archived)]
    public void ChangeStatus_accepts_every_status(ProductStatus status)
    {
        var product = Product.Create("Shirt", "shirt");

        product.ChangeStatus(status);

        product.Status.Should().Be(status);
    }

    [Fact]
    public void AssignCategory_is_idempotent()
    {
        var product = Product.Create("Shirt", "shirt");

        product.AssignCategory(10);
        product.AssignCategory(10);
        product.AssignCategory(20);

        product.Categories.Select(c => c.CategoryId).Should().BeEquivalentTo([10L, 20L]);
        product.Categories.Should().OnlyContain(c => c.ProductId == product.Id);
    }

    [Fact]
    public void RemoveCategory_removes_mapping_and_ignores_missing_one()
    {
        var product = Product.Create("Shirt", "shirt");
        product.AssignCategory(10);

        product.RemoveCategory(10);
        product.RemoveCategory(99);

        product.Categories.Should().BeEmpty();
    }
}
