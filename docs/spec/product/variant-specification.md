# Product Variant Specification

**Version:** 1.0\
**Source:** CommerceLab Product Catalog database design, Version 1.0\
**Parent document:** [product-management-specification.md](product-management-specification.md)
— shared API conventions, statuses, and the stock-service boundary apply.

## 1. Scope

Defines the API and business rules for product variants (SKUs) and the
attribute values assigned to them.

Owned tables: `product_variants`, `product_variant_attributes`.

Attribute *definitions* (e.g. "Color", "Size") are specified in
[attribute-specification.md](attribute-specification.md); variant images
in [image-specification.md](image-specification.md).

Variant status uses the Product/Variant status enum
(`Draft`/`Active`/`Inactive`/`Archived`). `Active` means catalog-active,
**not** in-stock.

## 2. Variant API

### POST `/api/v1/products/{productId}/variants`

``` json
{
  "sku": "TSHIRT-RED-L",
  "name": "Men's Casual T-Shirt - Red / L",
  "price": 29.99,
  "currency": "USD",
  "status": "Draft",
  "attributes": [
    { "attributeId": 1, "value": "Red" },
    { "attributeId": 2, "value": "L" }
  ]
}
```

Validation:

-   Parent product exists.
-   `sku` required, maximum 100 characters, globally unique.
-   `name` required, maximum 255 characters.
-   `price` required and compatible with `DECIMAL(12,2)`.
-   `currency` required, three-letter ISO 4217 code.
-   `status` valid product/variant status; default `Draft`.
-   Every attribute definition exists.
-   No attribute ID appears twice in one command.
-   Attribute value required, maximum 255 characters.

Creating or updating a variant **never creates or changes stock**.

### GET `/api/v1/variants/{variantId}`

Returns variant data and its attributes/images. Do not include stock
quantities.

### PUT `/api/v1/variants/{variantId}`

Applies the same validation as creation. SKU remains globally unique.
Price/status changes are catalog changes only and do not alter stock.

### POST `/api/v1/variants/{variantId}/status`

``` json
{ "status": "Active" }
```

Changes lifecycle status. `Active` means catalog-active, not in-stock.

### DELETE `/api/v1/variants/{variantId}`

Deletes the variant and its variant attributes/images atomically. The
parent product remains. Do not delete external inventory records; if
inventory references the SKU, deletion must follow an agreed
cross-service contract.

## 3. Variant Attribute Values

### PUT `/api/v1/variants/{variantId}/attributes/{attributeId}`

``` json
{ "value": "Red" }
```

Variant and attribute must exist; value is required and max 255
characters. Existing mapping is replaced. This is idempotent.

### DELETE `/api/v1/variants/{variantId}/attributes/{attributeId}`

Removes the mapping. The database composite key
`(variant_id, attribute_id)` guarantees at most one value for an
attribute on a variant.

## 4. Business Rules

1.  Variant SKU is globally unique.
2.  Every variant belongs to exactly one product.
3.  A variant may have zero or more attributes and variant images.
4.  A variant can have at most one value for a given attribute.
5.  Attribute definitions must exist before assignment.
6.  Variant operations never create, change, or delete stock.

## 5. Validation Matrix

| Entity | Field | Validation |
| --- | --- | --- |
| Variant | sku | Required; max 100; unique |
| Variant | name | Required; max 255 |
| Variant | price | Required; DECIMAL(12,2) |
| Variant | currency | Required; 3-letter ISO 4217 |
| Variant | status | Draft/Active/Inactive/Archived |
| Variant Attribute | attributeId | Required; existing definition; not repeated |
| Variant Attribute | value | Required; max 255 |

## 6. Transactions and Concurrency

-   Variant creation/update together with its attributes is one atomic
    command. If any attribute is invalid, nothing is persisted.
-   Variant deletion removes its attribute values and images in the same
    transaction.
-   SKU pre-checks give friendly errors, but the database unique
    constraint is authoritative; a violation is translated to
    `409 VARIANT_SKU_ALREADY_EXISTS`.
-   The `(variant_id, attribute_id)` composite key is authoritative for
    attribute values.

## 7. API Resource Matrix

| Operation | HTTP | Endpoint |
| --- | --- | --- |
| Create variant | POST | `/products/{id}/variants` |
| Get variant | GET | `/variants/{id}` |
| Update variant | PUT | `/variants/{id}` |
| Change variant status | POST | `/variants/{id}/status` |
| Delete variant | DELETE | `/variants/{id}` |
| Set variant attribute | PUT | `/variants/{variantId}/attributes/{attributeId}` |
| Remove variant attribute | DELETE | `/variants/{variantId}/attributes/{attributeId}` |

## 8. Open Items

-   Cross-service behavior when a SKU is already referenced by inventory
    (block delete, archive instead, or notify the stock service).
-   Whether a variant list endpoint (`GET /products/{id}/variants`) is
    required in addition to embedding variants in the product response.
-   Lifecycle transition policy between variant statuses.

## 9. Acceptance Criteria

-   Variant CRUD and lifecycle status work; SKUs are globally unique.
-   Variant price/currency validation is enforced.
-   Variant attributes reference existing definitions and cannot
    duplicate an attribute key.
-   Variant create/update with attributes is transactional.
-   No variant operation creates, updates, or deletes stock quantity or
    inventory state.
