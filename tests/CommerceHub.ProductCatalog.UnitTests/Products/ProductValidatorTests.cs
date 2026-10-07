using CommerceHub.ProductCatalog.Application.Products;
using CommerceHub.ProductCatalog.Domain.Products;
using CommerceHub.ProductCatalog.UnitTests.TestSupport;

namespace CommerceHub.ProductCatalog.UnitTests.Products;

public sealed class ProductValidatorTests
{
    private static ProductFields Valid(
        string? name = "Men's Casual T-Shirt",
        string? slug = "mens-casual-t-shirt",
        string? description = "Cotton casual T-shirt.",
        ProductStatus? status = null) => new(name, slug, description, status);

    [Fact]
    public void Valid_input_is_normalized_and_defaults_to_draft()
    {
        var result = ProductValidator.Validate(Valid(name: "  Shirt ", slug: " shirt ", description: "  d  "));

        result.ShouldSucceed();
        result.Value.Should().Be(new ValidProduct("Shirt", "shirt", "d", ProductStatus.Draft));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Name_is_required(string? name) =>
        ProductValidator.Validate(Valid(name: name)).ShouldFailWithValidation("'name' is required");

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Slug_is_required(string? slug) =>
        ProductValidator.Validate(Valid(slug: slug)).ShouldFailWithValidation("'slug' is required");

    [Fact]
    public void Name_max_length_is_255()
    {
        ProductValidator.Validate(Valid(name: new string('a', 255))).ShouldSucceed();
        ProductValidator.Validate(Valid(name: new string('a', 256))).ShouldFailWithValidation("'name' must not exceed 255");
    }

    [Fact]
    public void Slug_max_length_is_255()
    {
        ProductValidator.Validate(Valid(slug: new string('a', 255))).ShouldSucceed();
        ProductValidator.Validate(Valid(slug: new string('a', 256))).ShouldFailWithValidation("'slug' must not exceed 255");
    }

    [Theory]
    [InlineData("Uppercase")]
    [InlineData("has space")]
    [InlineData("double--hyphen")]
    [InlineData("-leading")]
    [InlineData("trailing-")]
    [InlineData("under_score")]
    public void Slug_must_be_lowercase_hyphenated(string slug) =>
        ProductValidator.Validate(Valid(slug: slug)).ShouldFailWithValidation("'slug' must be lowercase");

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Description_is_optional_and_blank_becomes_null(string? description)
    {
        var result = ProductValidator.Validate(Valid(description: description));

        result.ShouldSucceed();
        result.Value.Description.Should().BeNull();
    }

    [Theory]
    [InlineData(ProductStatus.Draft)]
    [InlineData(ProductStatus.Active)]
    [InlineData(ProductStatus.Inactive)]
    [InlineData(ProductStatus.Archived)]
    public void Every_defined_status_is_accepted(ProductStatus status) =>
        ProductValidator.Validate(Valid(status: status)).Value.Status.Should().Be(status);

    [Fact]
    public void Undefined_status_is_rejected() =>
        ProductValidator.Validate(Valid(status: (ProductStatus)99)).ShouldFailWith(ProductValidator.InvalidStatus);
}
