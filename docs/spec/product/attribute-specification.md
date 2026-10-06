# Attribute Definition Specification

**Version:** 1.0\
**Source:** CommerceLab Product Catalog database design, Version 1.0\
**Parent document:** [product-management-specification.md](product-management-specification.md)
— shared API conventions apply.

## 1. Scope

Defines the API and business rules for reusable attribute definitions
(e.g. `Color`, `Size`) that variants reference.

Owned table: `attributes`.

Assigning an attribute *value* to a variant is specified in
[variant-specification.md](variant-specification.md#3-variant-attribute-values).

## 2. Attribute Definition API

### POST `/api/v1/attributes`

``` json
{ "name": "Color", "code": "color" }
```

Validation: `name` required, max 100 characters. `code` required, max
100 characters, unique.

Duplicate code =\> `409 ATTRIBUTE_CODE_ALREADY_EXISTS`.

### GET `/api/v1/attributes`

Lists attribute definitions for variant-management clients.

### GET `/api/v1/attributes/{attributeId}`

Returns one definition.

### PUT `/api/v1/attributes/{attributeId}`

Updates name/code using the same validation. Changing a definition must
not implicitly change existing variant values.

### DELETE `/api/v1/attributes/{attributeId}`

The schema does not define delete behavior for referenced attributes.
Recommended: reject while referenced (`409 ATTRIBUTE_IN_USE`) rather
than silently destroying variant characteristics. Confirm before
implementation.

## 3. Business Rules

1.  Attribute `code` is globally unique.
2.  Attribute definitions must exist before they are assigned to a
    variant.
3.  Updating a definition never rewrites existing variant values.
4.  A referenced definition is not deleted implicitly (recommended).

## 4. Validation Matrix

| Entity | Field | Validation |
| --- | --- | --- |
| Attribute | name | Required; max 100 |
| Attribute | code | Required; max 100; unique |

## 5. Concurrency

Code pre-checks give friendly errors, but the database unique constraint
is authoritative; a violation is translated to
`409 ATTRIBUTE_CODE_ALREADY_EXISTS`.

## 6. API Resource Matrix

| Operation | HTTP | Endpoint |
| --- | --- | --- |
| Create attribute | POST | `/attributes` |
| List attributes | GET | `/attributes` |
| Get attribute | GET | `/attributes/{id}` |
| Update attribute | PUT | `/attributes/{id}` |
| Delete attribute | DELETE | `/attributes/{id}` |

## 7. Open Items

-   Delete policy for attributes referenced by variants (recommended:
    `409 ATTRIBUTE_IN_USE`).
-   Case sensitivity/collation of `code` uniqueness.
-   Whether `code` is immutable after first use.

## 8. Acceptance Criteria

-   Attribute definition CRUD works with the documented validations.
-   Attribute codes are unique; conflicts return a stable `409`.
-   Updating a definition does not change existing variant values.
-   A referenced attribute cannot be deleted silently.
