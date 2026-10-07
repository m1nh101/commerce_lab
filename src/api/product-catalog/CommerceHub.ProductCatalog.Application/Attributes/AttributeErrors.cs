using CommerceHub.ProductCatalog.Application.Common;

namespace CommerceHub.ProductCatalog.Application.Attributes;

public static class AttributeErrors
{
    public static readonly Error NotFound =
        new("ATTRIBUTE_NOT_FOUND", "Attribute not found.", ErrorType.NotFound);

    public static readonly Error CodeConflict =
        new("ATTRIBUTE_CODE_ALREADY_EXISTS", "An attribute with this code already exists.", ErrorType.Conflict);

    public static readonly Error InUse =
        new("ATTRIBUTE_IN_USE", "Cannot delete an attribute that is assigned to variants.", ErrorType.Conflict);
}
