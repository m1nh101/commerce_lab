# Product Management Service Specification

**Version:** 1.1\
**Source:** CommerceLab Product Catalog database design, Version 1.0\
**Boundary:** Product catalog management only\
**Stock:** Managed by a separate service and explicitly out of scope.

### Related documents

| Document | Covers |
| --- | --- |
| [variant-specification.md](variant-specification.md) | Variants/SKUs and variant attribute values |
| [attribute-specification.md](attribute-specification.md) | Reusable attribute definitions |
| [image-specification.md](image-specification.md) | Product and variant image references |
| [categories_specification.md](categories_specification.md) | Category hierarchy |

## 1. Scope

The Product Management service owns products, variants/SKUs, variant
attributes, attribute definitions, categories, product-category mappings,
and product/variant image references. It does **not** own inventory
quantity, reservations, warehouse stock, stock movements, availability, or
other inventory state.

The database defines `products`, `product_variants`, `attributes`,
`product_variant_attributes`, `categories`, `product_categories`, and
`product_images`. Products have variants, categories, and images; variants
have attributes and may have variant-specific images; categories are
hierarchical.

**This document** specifies the shared conventions, the Product resource,
product-category mappings, and the stock-service boundary. Every other
entity is specified in the related documents listed above.

## 2. Statuses

### Product and Variant

| Value | Name |
| ---: | --- |
| 0 | Draft |
| 1 | Active |
| 2 | Inactive |
| 3 | Archived |

The API should expose enum names rather than database integers. Category
statuses are defined in [categories_specification.md](categories_specification.md).

## 3. API Conventions

> These conventions apply to **all** product-catalog specifications
> (product, variant, attribute, image) unless a document states otherwise.

Base path: `/api/v1`\
Content type: `application/json`.

Identifiers: product/variant IDs are UUIDs; category/attribute IDs are
numeric.

Recommended errors:

``` json
{
  "code": "PRODUCT_SLUG_ALREADY_EXISTS",
  "message": "A product with the specified slug already exists.",
  "details": { "field": "slug" }
}
```

Validation errors may use:

``` json
{
  "code": "VALIDATION_ERROR",
  "message": "One or more fields are invalid.",
  "errors": [
    { "field": "name", "code": "REQUIRED", "message": "Name is required." }
  ]
}
```

Use `201` for creation, `200` for successful reads/updates, `204` for
successful deletes without a body, `400` for malformed commands, `404`
for missing resources, `409` for uniqueness/state conflicts, and `422`
for valid requests that violate domain validation.

## 4. Product API

### POST `/api/v1/products`

Creates a product.

Request:

``` json
{
  "name": "Men's Casual T-Shirt",
  "slug": "mens-casual-t-shirt",
  "description": "Cotton casual T-shirt.",
  "status": "Draft"
}
```

Validation:

-   `name` required, maximum 255 characters.
-   `slug` required, maximum 255 characters, globally unique.
-   `description` optional.
-   `status` valid product status; default `Draft`.
-   `id`, timestamps are server-generated.

Duplicate slug =\> `409 PRODUCT_SLUG_ALREADY_EXISTS`.

### GET `/api/v1/products/{productId}`

Returns product catalog data, optionally including
[variants](variant-specification.md) and categories (see §5).
[Images](image-specification.md) are returned per variant, not at the
product level. **Do not return stock quantities from this service.**

### GET `/api/v1/products`

Recommended query parameters: `page`, `pageSize`, `status`,
`categoryId`, `search`, `slug`, `sort`.

Pagination/search semantics and limits are not defined by the source
schema and must be agreed during implementation.

### PUT `/api/v1/products/{productId}`

Replaces product fields. Product must exist; all create validations
apply; a changed slug must remain unique. ID and creation timestamp
cannot change.

### PATCH `/api/v1/products/{productId}`

Optional partial-update endpoint. Omitted fields remain unchanged;
explicitly nullable fields such as `description` may be cleared.

### POST `/api/v1/products/{productId}/status`

``` json
{ "status": "Active" }
```

Validates the product status. A stricter transition matrix is a service
recommendation, not defined by the database, and must be confirmed
before enforcement.

### DELETE `/api/v1/products/{productId}`

The database specifies cascade deletion for variants, product-category
mappings, and product images. Execute the deletion atomically. Never
delete stock data owned by another service. If historical retention is
required, use `Archived` instead.

## 5. Product Categories

Manages the `product_categories` mapping between a product and existing
categories. The category resource itself (create, hierarchy, delete) is
defined in [categories_specification.md](categories_specification.md).

### PUT `/api/v1/products/{productId}/categories/{categoryId}`

Assigns a category. Product/category must exist. Duplicate assignment is
idempotent; the composite primary key prevents duplicates.

### DELETE `/api/v1/products/{productId}/categories/{categoryId}`

Removes the mapping only. Idempotent behavior is recommended.

### PUT `/api/v1/products/{productId}/categories`

Optional bulk replacement:

``` json
{ "categoryIds": [10, 20, 30] }
```

Validate all categories first, then remove obsolete mappings/add missing
mappings in one transaction. If any category is invalid, make no
changes.

## 6. Core Business Rules

1.  Product slug is globally unique.
2.  A product may have multiple variants, categories, and images.
3.  Product/category mappings are unique.
4.  Deleting a product cascades to its variants, category mappings, and
    images — never to categories themselves or to stock data.
5.  `created_at`/`updated_at` are UTC timestamps; update timestamp
    changes when the persisted entity changes.
6.  Status values must match the schema-defined enums.
7.  Catalog operations never manage inventory quantities.

Variant, attribute, and image rules are listed in their own documents.

## 7. Validation Matrix

| Entity | Field | Validation |
| --- | --- | --- |
| Product | name | Required; max 255 |
| Product | slug | Required; max 255; unique |
| Product | description | Optional |
| Product | status | Draft/Active/Inactive/Archived |
| Product Category | categoryId | Required; existing category |
| Product Category | categoryIds (bulk) | All must exist; otherwise no change |

## 8. Transaction Rules

Operations that change multiple catalog tables must be atomic,
especially:

-   product creation with variants/categories/images;
-   replacing product categories;
-   deleting a product and its owned catalog children.

Validate all referenced resources before committing. A failed child
operation must roll back the complete command. Entity-specific
transactions are described in the related documents.

## 9. Concurrency and Database Constraints

Application pre-checks for unique values (product slug, variant SKU,
attribute code) are useful for friendly errors but are not sufficient.
Concurrent requests can pass the pre-check simultaneously. Database
unique constraints remain authoritative and unique-constraint violations
must be translated into the corresponding `409` domain error.

The schema's composite key also remains authoritative for
`product_categories`.

## 10. Stock-Service Boundary

### Owned by the Product Management service

-   Product identity/name/description/slug/status
-   Variant identity/SKU/name/price/currency/status
-   Variant attributes and attribute definitions
-   Categories and product-category mappings
-   Image references

### Not owned

-   Quantity on hand
-   Available/reserved quantity
-   Warehouse/location inventory
-   Stock movements/adjustments/transfers
-   Reservations
-   Reorder thresholds
-   Inventory availability

Do not put `stock`, `availableStock`, or `reservedStock` in
product/variant write models. A product page needing inventory data
should compose catalog data with the separate stock service.

## 11. API Resource Matrix

| Operation | HTTP | Endpoint |
| --- | --- | --- |
| Create product | POST | `/products` |
| List products | GET | `/products` |
| Get product | GET | `/products/{id}` |
| Update product | PUT | `/products/{id}` |
| Patch product | PATCH | `/products/{id}` |
| Change product status | POST | `/products/{id}/status` |
| Delete product | DELETE | `/products/{id}` |
| Assign category | PUT | `/products/{productId}/categories/{categoryId}` |
| Remove category | DELETE | `/products/{productId}/categories/{categoryId}` |
| Replace categories | PUT | `/products/{productId}/categories` |

See also: [variant](variant-specification.md#7-api-resource-matrix),
[attribute](attribute-specification.md#6-api-resource-matrix),
[image](image-specification.md#5-api-resource-matrix), and
[category](categories_specification.md) endpoints.

## 12. Explicitly Unspecified by the Source Schema

The database source does not define exact pagination/search semantics,
authorization/roles, audit history, lifecycle transition policy, or case
sensitivity/collation for unique strings.

Those items should be confirmed as business requirements rather than
silently treated as database-derived rules.

## 13. Acceptance Criteria

-   Product CRUD and lifecycle status work with all documented
    validations.
-   Product slugs are unique.
-   Product/category mappings are unique and can be replaced atomically.
-   Database uniqueness conflicts are converted into stable `409`
    errors.
-   Multi-entity catalog commands are transactional.
-   No API operation creates, updates, or deletes stock quantity or
    inventory state.
-   Product deletion cannot cascade into the separately owned stock
    service.

## 14. Source-Derived Data Constraints

The source schema specifies: UUIDv7-style product IDs; product
strings and statuses; globally unique product slugs; composite uniqueness
for product categories; UTC `TIMESTAMPTZ` audit timestamps; and
product-owned cascades for variants, category mappings, and images.

Where this document proposes behavior not explicitly stated by the
schema, it is marked as recommended or requiring confirmation.
