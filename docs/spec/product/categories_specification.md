# API & CRUD Specification: Categories Module

## 1. Overview
This document defines the RESTful API endpoints and business logic for managing the `categories` resource. The table supports a self-referencing hierarchical taxonomy (parent-child relationship).

### Database Schema Reference
* **Table**: `categories`
---

## 2. Business Rules & Validation

1. **Unique Slug**: `slug` must be unique across all categories. If omitted during creation, it should be auto-generated from `name` (kebab-case).
2. **Parent Reference Validation**: `parent_id`, if provided, must correspond to an existing record in `categories`.
3. **Circular Reference Prevention**: A category cannot be set as its own parent (`parent_id != id`), nor can `parent_id` be set to any of its direct or indirect descendant categories.
4. **Deletion Behavior**:
   * **Restrict/Prevent**: Deleting a category with active children should return a `409 Conflict` unless explicit soft-deletion or cascade behavior is defined.
   * **Re-assignment Option**: Optional query param `reassign_children_to` to move subcategories before deletion.
5. **Allowed Statuses**: `active`, `hidden`, `archived`. Default is `active`.

---

## 3. API Endpoints Overview

| Method | Endpoint | Description |
| :--- | :--- | :--- |
| `POST` | `/api/v1/categories` | Create a new category |
| `GET` | `/api/v1/categories` | List categories (supports tree view, pagination, search) |
| `GET` | `/api/v1/categories/{id_or_slug}` | Retrieve a single category |
| `PUT` / `PATCH` | `/api/v1/categories/{id}` | Update an existing category |
| `DELETE` | `/api/v1/categories/{id}` | Delete a category |

---

## 4. Endpoint Specifications

### 4.1 Create Category (CREATE)
* **HTTP Method**: `POST`
* **Path**: `/api/v1/categories`

#### Request Body
```json
{
  "parent_id": 1,
  "name": "Men's Shirts",
  "slug": "mens-shirts",
  "status": "active"
}
```

#### Validation Rules
* `name`: Required, max 255 characters.
* `slug`: Optional (auto-generated from `name` if not provided), max 255 characters, must be unique, regex pattern: `^[a-z0-9]+(?:-[a-z0-9]+)*$`.
* `parent_id`: Optional (must exist in `categories` table if provided).
* `status`: Optional, default `active`. Must be one of `['active', 'hidden', 'archived']`.

#### Responses
* **201 Created**
  ```json
  {
    "success": true,
    "data": {
      "id": 12,
      "parent_id": 1,
      "name": "Men's Shirts",
      "slug": "mens-shirts",
      "status": "active"
    }
  }
  ```
* **400 Bad Request**: Invalid parameters or validation error.
* **409 Conflict**: `slug` already exists.

---

### 4.2 Read Categories List (READ)
* **HTTP Method**: `GET`
* **Path**: `/api/v1/categories`

#### Query Parameters
* `tree` (boolean, default: `false`): If `true`, returns data as a nested tree structure.
* `parent_id` (integer, optional): Filter by direct parent ID (`null` for top-level categories).
* `status` (string, optional): Filter by status (`active`, `hidden`, `archived`).
* `search` (string, optional): Search term against `name` or `slug`.
* `page` (integer, default: `1`): Page number (ignored if `tree=true`).
* `limit` (integer, default: `20`): Page size (ignored if `tree=true`).

#### Responses
* **200 OK (Flat / Paginated)**
  ```json
  {
    "success": true,
    "data": [
      {
        "id": 1,
        "parent_id": null,
        "name": "Clothing",
        "slug": "clothing",
        "status": "active"
      },
      {
        "id": 5,
        "parent_id": 1,
        "name": "Men's Wear",
        "slug": "mens-wear",
        "status": "active"
      }
    ],
    "pagination": {
      "total": 45,
      "page": 1,
      "limit": 20,
      "total_pages": 3
    }
  }
  ```

* **200 OK (Tree Format - `?tree=true`)**
  ```json
  {
    "success": true,
    "data": [
      {
        "id": 1,
        "parent_id": null,
        "name": "Clothing",
        "slug": "clothing",
        "status": "active",
        "children": [
          {
            "id": 5,
            "parent_id": 1,
            "name": "Men's Wear",
            "slug": "mens-wear",
            "status": "active",
            "children": []
          }
        ]
      }
    ]
  }
  ```

---

### 4.3 Get Single Category (READ)
* **HTTP Method**: `GET`
* **Path**: `/api/v1/categories/{id_or_slug}`

#### Path Parameters
* `id_or_slug` (string/integer, required): Numeric category ID or category `slug`.

#### Responses
* **200 OK**
  ```json
  {
    "success": true,
    "data": {
      "id": 5,
      "parent_id": 1,
      "name": "Men's Wear",
      "slug": "mens-wear",
      "status": "active"
    }
  }
  ```
* **404 Not Found**: Category does not exist.

---

### 4.4 Update Category (UPDATE)
* **HTTP Method**: `PATCH` / `PUT`
* **Path**: `/api/v1/categories/{id}`

#### Request Body
```json
{
  "parent_id": 2,
  "name": "Men's Apparel",
  "status": "active"
}
```

#### Validation & Logic Rules
* `parent_id` cannot equal `id`.
* `parent_id` cannot be a descendant of `id` (prevents cyclic hierarchy).
* `slug` uniqueness rule applies if updated.

#### Responses
* **200 OK**
  ```json
  {
    "success": true,
    "data": {
      "id": 5,
      "parent_id": 2,
      "name": "Men's Apparel",
      "slug": "mens-wear",
      "status": "active"
    }
  }
  ```
* **400 Bad Request**: Validation failed or circular parent assignment attempted.
* **404 Not Found**: Category ID not found.
* **409 Conflict**: Updated `slug` already exists.

---

### 4.5 Delete Category (DELETE)
* **HTTP Method**: `DELETE`
* **Path**: `/api/v1/categories/{id}`

#### Query Parameters
* `reassign_children_to` (integer, optional): ID of target parent category to re-assign child categories to.

#### Logic Rules
1. Check if category has child categories (`WHERE parent_id = {id}`).
2. If children exist and `reassign_children_to` is provided, update children `parent_id` to `reassign_children_to`.
3. If children exist and `reassign_children_to` is NOT provided, reject deletion (`409 Conflict`).

#### Responses
* **200 OK**
  ```json
  {
    "success": true,
    "message": "Category deleted successfully."
  }
  ```
* **409 Conflict**
  ```json
  {
    "success": false,
    "error": {
      "code": "HAS_CHILDREN",
      "message": "Cannot delete category with active subcategories. Provide 'reassign_children_to' parameter or delete child categories first."
    }
  }
  ```
* **404 Not Found**: Category ID not found.