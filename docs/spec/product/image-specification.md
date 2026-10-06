# Product Image Specification

**Version:** 1.1\
**Source:** CommerceLab Product Catalog database design, Version 1.0\
**Parent document:** [product-management-specification.md](product-management-specification.md)
— shared API conventions apply.

## 1. Scope

Defines the API and business rules for catalog image references attached
to a product, optionally scoped to one of its variants.

Owned table: `product_images`.

Image files are stored in **Amazon S3**. The service issues presigned
upload URLs; the client uploads the file **directly to S3** (file bytes
never pass through the service) and then registers the resulting URL as
an image reference. The `product_images` table stores image
**references** (URLs) only. Physical deletion of S3 objects is outside
this scope.

> **Storage note:** the product-image S3 bucket is **public-read** —
> uploaded objects are served directly by their public URL. To avoid data
> leaks, this bucket is dedicated to product images only. No other data
> (private, customer, order, or internal files) may ever be stored in it.

## 2. Image API

### POST `/api/v1/products/{productId}/images/upload-url`

Returns a presigned S3 URL for uploading one product image.

Request:

``` json
{
  "fileName": "main.jpg",
  "contentType": "image/jpeg",
  "contentLength": 245678
}
```

Response `200`:

``` json
{
  "uploadUrl": "https://<bucket>.s3.<region>.amazonaws.com/products/<productId>/<uuid>.jpg?X-Amz-...",
  "method": "PUT",
  "headers": { "Content-Type": "image/jpeg" },
  "objectKey": "products/<productId>/<uuid>.jpg",
  "imageUrl": "https://<bucket>.s3.<region>.amazonaws.com/products/<productId>/<uuid>.jpg",
  "expiresAt": "2026-10-06T10:15:00Z"
}
```

Rules:

-   Product must exist; otherwise `404`.
-   The URL always targets the dedicated product-image bucket. Clients
    cannot choose the bucket, key, or path.
-   The object key is **server-generated** as
    `products/{productId}/{uuid}.{ext}`; `fileName` is used only to
    derive the extension.
-   The presigned URL allows a single `PUT` of that key only, with the
    signed `Content-Type` (and length where supported), and expires
    quickly (recommended 15 minutes, configurable).
-   `contentType` must be an allowed image type (recommended
    `image/jpeg`, `image/png`, `image/webp`); otherwise
    `422 UNSUPPORTED_IMAGE_CONTENT_TYPE`.
-   `contentLength` is required, greater than 0, and at most the
    configured maximum (recommended 10 MB); otherwise
    `422 IMAGE_TOO_LARGE`.
-   Issuing a URL creates **no** `product_images` row.

Upload flow:

1.  Request a presigned URL from this endpoint.
2.  `PUT` the file to `uploadUrl` with the returned `headers`.
3.  Register the image by calling `POST .../images` with `imageUrl` as
    `url`. Objects that are uploaded but never registered are orphans.

### POST `/api/v1/products/{productId}/images`

``` json
{
  "url": "https://cdn.example.com/products/tshirt/main.jpg",
  "sortOrder": 0
}
```

Product must exist; URL required, max 2048 characters; `sortOrder`
defaults to 0. Creates a general product image (`variantId = null`).

### POST `/api/v1/variants/{variantId}/images`

Same request shape. Resolve the variant's parent and store both
`product_id` and `variant_id`.

### PUT `/api/v1/products/{productId}/images/{imageId}`

Updates URL/order. Image must belong to the specified product.

### DELETE `/api/v1/products/{productId}/images/{imageId}`

Deletes only the catalog media reference. Physical object deletion is
outside the schema unless a separate media contract exists.

## 3. Business Rules

1.  A product may have multiple images.
2.  A general image has `variantId = null`; a variant image has a
    non-null variant ID.
3.  **Image integrity rule:** if `variant_id` is non-null, its variant
    must belong to the same `product_id`. The database has separate
    foreign keys but does not itself enforce this composite ownership
    rule; the service must enforce it.
4.  Images are deleted with their product (cascade) and with their
    variant.
5.  Image files are stored in Amazon S3.
6.  The product-image bucket is public-read and holds product images
    only; no other data may be stored in it.
7.  Presigned upload URLs are single-object, short-lived, and use
    server-generated keys.

## 4. Validation Matrix

| Entity | Field | Validation |
| --- | --- | --- |
| Image | url | Required; max 2048 |
| Image | sortOrder | Integer; default 0 |
| Image | variantId | Optional; must belong to the same product |
| Upload URL | fileName | Required; extension must match an allowed image type |
| Upload URL | contentType | Required; allowed image type |
| Upload URL | contentLength | Required; > 0; ≤ configured maximum |

## 5. API Resource Matrix

| Operation | HTTP | Endpoint |
| --- | --- | --- |
| Get presigned upload URL | POST | `/products/{productId}/images/upload-url` |
| Add product image | POST | `/products/{productId}/images` |
| Add variant image | POST | `/variants/{variantId}/images` |
| Update image | PUT | `/products/{productId}/images/{imageId}` |
| Delete image | DELETE | `/products/{productId}/images/{imageId}` |

## 6. Open Items

-   Final maximum file size and allowed content types.
-   Presigned URL expiry duration.
-   Cleanup of orphaned S3 objects (uploaded but never registered).
-   Whether deleting an image reference should also delete its S3
    object.
-   Whether registered `url` values must be restricted to the
    product-image bucket host (recommended).
-   Whether a CDN is placed in front of the bucket.
-   Whether an image can be moved between a product and a variant via
    update.

## 7. Acceptance Criteria

-   Product and variant images can be added, updated, and deleted.
-   Variant-image ownership is validated against the parent product.
-   Deleting an image removes only the catalog reference.
-   A presigned upload URL is issued only for an existing product.
-   The object key is server-generated under `products/{productId}/`.
-   Requests with a disallowed content type or oversized file are
    rejected with `422`.
-   Uploaded files are stored only in the dedicated public product-image
    bucket.
