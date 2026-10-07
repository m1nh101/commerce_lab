using CommerceHub.ProductCatalog.Application.Common;

namespace CommerceHub.ProductCatalog.UnitTests.TestSupport;

internal static class ResultAssertions
{
    public static void ShouldFailWith(this Result result, Error expected)
    {
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(expected);
    }

    public static void ShouldFailWithValidation(this Result result, string messageFragment)
    {
        result.IsSuccess.Should().BeFalse();
        result.Error!.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be("VALIDATION_ERROR");
        result.Error.Message.Should().Contain(messageFragment);
    }

    public static void ShouldSucceed(this Result result) =>
        result.IsSuccess.Should().BeTrue($"expected success but got {result.Error}");
}
