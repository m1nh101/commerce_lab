-- =============================================================================
-- CommerceHub — Product Catalog seed data
-- Schema : docs/database/schemas/product-schema.md
-- Target : PostgreSQL 18+ (uses uuidv7()), database "product_catalog"
--
-- Produces (deterministic — re-running yields the same shape and values):
--   attributes                   6
--   categories                  32  (8 roots + 24 leaves)
--   products                  1200
--   product_categories        2400  (leaf + its root per product)
--   product_variants          4050
--   product_variant_attributes 7150
--   product_images            6450  (2 general per product + 1 per variant)
--
-- WARNING: this script TRUNCATEs every catalog table before inserting.
-- Usage  : psql "$CONNECTION_STRING" -f docs/database/seeds/product-catalog-seed.sql
-- =============================================================================

BEGIN;

TRUNCATE TABLE
    product_images,
    product_variant_attributes,
    product_variants,
    product_categories,
    products,
    categories,
    attributes
RESTART IDENTITY CASCADE;

-- -----------------------------------------------------------------------------
-- 1. attributes
-- -----------------------------------------------------------------------------
INSERT INTO attributes (id, name, code) OVERRIDING SYSTEM VALUE VALUES
    (1, 'Color',            'color'),
    (2, 'Size',             'size'),
    (3, 'Storage Capacity', 'storage'),
    (4, 'Material',         'material'),
    (5, 'Memory (RAM)',     'ram'),
    (6, 'Volume',           'volume');

-- -----------------------------------------------------------------------------
-- 2. categories (adjacency list; status 0 = Active, 1 = Hidden, 2 = Archived)
-- -----------------------------------------------------------------------------
INSERT INTO categories (id, parent_id, name, slug, status) OVERRIDING SYSTEM VALUE VALUES
    -- roots
    ( 1, NULL, 'Electronics',          'electronics',          0),
    ( 2, NULL, 'Men''s Fashion',       'mens-fashion',         0),
    ( 3, NULL, 'Women''s Fashion',     'womens-fashion',       0),
    ( 4, NULL, 'Home & Kitchen',       'home-kitchen',         0),
    ( 5, NULL, 'Sports & Outdoors',    'sports-outdoors',      0),
    ( 6, NULL, 'Beauty & Personal Care','beauty-personal-care', 0),
    ( 7, NULL, 'Books & Stationery',   'books-stationery',     0),
    ( 8, NULL, 'Toys & Games',         'toys-games',           1),
    -- Electronics
    ( 9, 1, 'Smartphones',             'smartphones',          0),
    (10, 1, 'Laptops',                 'laptops',              0),
    (11, 1, 'Headphones',              'headphones',           0),
    -- Men's Fashion
    (12, 2, 'Men''s T-Shirts',         'mens-t-shirts',        0),
    (13, 2, 'Men''s Jeans',            'mens-jeans',           0),
    (14, 2, 'Men''s Sneakers',         'mens-sneakers',        0),
    -- Women's Fashion
    (15, 3, 'Women''s Dresses',        'womens-dresses',       0),
    (16, 3, 'Women''s Tops',           'womens-tops',          0),
    (17, 3, 'Women''s Handbags',       'womens-handbags',      0),
    -- Home & Kitchen
    (18, 4, 'Cookware',                'cookware',             0),
    (19, 4, 'Bedding',                 'bedding',              0),
    (20, 4, 'Home Decor',              'home-decor',           0),
    -- Sports & Outdoors
    (21, 5, 'Fitness Equipment',       'fitness-equipment',    0),
    (22, 5, 'Camping Gear',            'camping-gear',         0),
    (23, 5, 'Activewear',              'activewear',           0),
    -- Beauty & Personal Care
    (24, 6, 'Skincare',                'skincare',             0),
    (25, 6, 'Fragrances',              'fragrances',           0),
    (26, 6, 'Hair Care',               'hair-care',            2),
    -- Books & Stationery
    (27, 7, 'Notebooks & Journals',    'notebooks-journals',   0),
    (28, 7, 'Writing Instruments',     'writing-instruments',  0),
    (29, 7, 'Desk Accessories',        'desk-accessories',     0),
    -- Toys & Games
    (30, 8, 'Board Games',             'board-games',          1),
    (31, 8, 'Building Blocks',         'building-blocks',      1),
    (32, 8, 'Puzzles',                 'puzzles',              1);

-- -----------------------------------------------------------------------------
-- 3. Generation templates (temporary, dropped on commit)
--    kind drives the variant matrix:
--      apparel  -> color x size        electronics -> color x storage (+ram)
--      volume   -> volume               material    -> material x color
--      simple   -> color only
-- -----------------------------------------------------------------------------
CREATE TEMP TABLE seed_template (
    t_no        int PRIMARY KEY,
    category_id bigint NOT NULL,
    root_id     bigint NOT NULL,
    noun        text   NOT NULL,
    kind        text   NOT NULL,
    brands      text[] NOT NULL,
    min_price   numeric(12,2) NOT NULL,
    max_price   numeric(12,2) NOT NULL
) ON COMMIT DROP;

INSERT INTO seed_template VALUES
    ( 0,  9, 1, 'Smartphone',          'electronics', ARRAY['Novatek','Zentro','Lumix One','Aerion'],   299, 1299),
    ( 1, 10, 1, 'Laptop',              'electronics', ARRAY['Corebyte','Vantix','Nimbus','Orbis'],      599, 2499),
    ( 2, 11, 1, 'Wireless Headphones', 'simple',      ARRAY['SoundCore','Auralis','Bassline','Echo'],    39,  399),
    ( 3, 12, 2, 'Crew Neck T-Shirt',   'apparel',     ARRAY['Northwind','Urban Fox','Basic Co.','Tidal'], 12,   45),
    ( 4, 13, 2, 'Slim Fit Jeans',      'apparel',     ARRAY['Denim Lab','Riverside','Indigo & Co.'],     35,  120),
    ( 5, 14, 2, 'Running Sneakers',    'apparel',     ARRAY['Stride','Pacer','Velo','Kinetic'],          49,  189),
    ( 6, 15, 3, 'Midi Dress',          'apparel',     ARRAY['Maison Lune','Petal','Aurelle'],            29,  159),
    ( 7, 16, 3, 'Linen Blouse',        'apparel',     ARRAY['Petal','Coastline','Aurelle'],              19,   79),
    ( 8, 17, 3, 'Leather Tote Bag',    'material',    ARRAY['Carryall','Maison Lune','Vessel'],          59,  349),
    ( 9, 18, 4, 'Non-Stick Frying Pan','simple',      ARRAY['ChefLine','Hearth','IronLeaf'],             19,  129),
    (10, 19, 4, 'Cotton Duvet Cover',  'apparel',     ARRAY['Dreamwell','Linen House','Nestle'],         39,  199),
    (11, 20, 4, 'Ceramic Vase',        'material',    ARRAY['Atelier','Kiln & Co.','Hearth'],            15,   89),
    (12, 21, 5, 'Adjustable Dumbbell', 'simple',      ARRAY['IronPeak','FlexFit','Titan'],               49,  399),
    (13, 22, 5, 'Camping Tent',        'simple',      ARRAY['Trailhead','Summit','Wildpine'],            79,  549),
    (14, 23, 5, 'Training Leggings',   'apparel',     ARRAY['FlexFit','Kinetic','Pacer'],                25,   95),
    (15, 24, 6, 'Hydrating Serum',     'volume',      ARRAY['Glowra','Purelle','Dermis'],                12,   79),
    (16, 25, 6, 'Eau de Parfum',       'volume',      ARRAY['Noir & Co.','Velour','Essenza'],            39,  189),
    (17, 26, 6, 'Repair Shampoo',      'volume',      ARRAY['Silkara','Purelle','Botanica'],              8,   35),
    (18, 27, 7, 'Dotted Journal',      'material',    ARRAY['Paperline','Inkwell','Folio'],               6,   35),
    (19, 28, 7, 'Fountain Pen',        'simple',      ARRAY['Inkwell','Scriptor','Nib & Co.'],           15,  220),
    (20, 29, 7, 'Desk Organizer',      'material',    ARRAY['Folio','Deskmate','Oakline'],               12,   69),
    (21, 30, 8, 'Strategy Board Game', 'simple',      ARRAY['Meeple Works','Tabletop Co.','Gamecraft'],  19,   89),
    (22, 31, 8, 'Building Block Set',  'simple',      ARRAY['Brickly','BuildIt','Blocktopia'],           15,  199),
    (23, 32, 8, '1000-Piece Puzzle',   'simple',      ARRAY['Puzzlecraft','Tessera','Gamecraft'],        12,   39);

CREATE TEMP TABLE seed_product (
    n       int  PRIMARY KEY,
    id      uuid NOT NULL DEFAULT uuidv7(),
    t_no    int  NOT NULL,
    brand   text NOT NULL,
    adj     text NOT NULL,
    created timestamptz NOT NULL
) ON COMMIT DROP;

WITH adjectives AS (
    SELECT ARRAY['Classic','Premium','Essential','Pro','Ultra','Eco','Signature',
                 'Everyday','Deluxe','Compact','Vintage','Modern','Lite','Elite',
                 'Urban','Heritage','Prime','Studio','Nova','Aero'] AS a
)
INSERT INTO seed_product (n, t_no, brand, adj, created)
SELECT g.n,
       g.n % 24,
       t.brands[1 + (g.n / 24) % array_length(t.brands, 1)],
       adjectives.a[1 + (g.n * 7) % 20],
       TIMESTAMPTZ '2025-01-01 00:00:00+00' + make_interval(mins => g.n * 397)
FROM generate_series(1, 1200) AS g(n)
JOIN seed_template t ON t.t_no = g.n % 24
CROSS JOIN adjectives
ORDER BY g.n;

-- -----------------------------------------------------------------------------
-- 4. products (status 0 = Draft, 1 = Active, 2 = Inactive, 3 = Archived)
--    distribution ≈ 80% Active, 8% Draft, 7% Inactive, 5% Archived
-- -----------------------------------------------------------------------------
INSERT INTO products (id, name, slug, description, status, created_at, updated_at)
SELECT p.id,
       p.brand || ' ' || p.adj || ' ' || t.noun,
       regexp_replace(lower(p.brand || ' ' || p.adj || ' ' || t.noun), '[^a-z0-9]+', '-', 'g')
           || '-' || lpad(p.n::text, 4, '0'),
       CASE WHEN p.n % 15 = 0 THEN NULL
            ELSE 'The ' || p.adj || ' ' || t.noun || ' by ' || p.brand
                 || ' combines thoughtful design with dependable quality. '
                 || 'Built for everyday use and backed by a 12-month warranty.'
       END,
       CASE WHEN p.n % 100 < 80 THEN 1
            WHEN p.n % 100 < 88 THEN 0
            WHEN p.n % 100 < 95 THEN 2
            ELSE 3
       END,
       p.created,
       p.created + make_interval(days => p.n % 90, hours => p.n % 24)
FROM seed_product p
JOIN seed_template t ON t.t_no = p.t_no;

-- -----------------------------------------------------------------------------
-- 5. product_categories (leaf category + its root category)
-- -----------------------------------------------------------------------------
INSERT INTO product_categories (product_id, category_id)
SELECT p.id, t.category_id FROM seed_product p JOIN seed_template t ON t.t_no = p.t_no
UNION ALL
SELECT p.id, t.root_id     FROM seed_product p JOIN seed_template t ON t.t_no = p.t_no;

-- -----------------------------------------------------------------------------
-- 6. product_variants + product_variant_attributes
--    Each product gets a variant matrix based on its template kind.
--    Attribute options are rotated by product number for variety.
-- -----------------------------------------------------------------------------
CREATE TEMP TABLE seed_variant (
    id         uuid NOT NULL DEFAULT uuidv7(),
    product_n  int  NOT NULL,
    product_id uuid NOT NULL,
    v_no       int  NOT NULL,
    attrs      jsonb NOT NULL     -- { "<attribute_id>": "<value>" }
) ON COMMIT DROP;

WITH opt AS (
    SELECT ARRAY['Black','White','Navy','Red','Grey','Green','Beige','Blue','Pink','Silver'] AS colors,
           ARRAY['XS','S','M','L','XL','XXL']                AS sizes,
           ARRAY['128GB','256GB','512GB','1TB']               AS storages,
           ARRAY['8GB','16GB','32GB']                         AS rams,
           ARRAY['Leather','Canvas','Ceramic','Bamboo','Linen','Recycled Paper'] AS materials,
           ARRAY['30ml','50ml','100ml','250ml']               AS volumes
),
matrix AS (
    -- apparel: 2 colors x 3 sizes = 6
    SELECT p.n, p.id, jsonb_build_object(
               '1', o.colors[1 + (p.n + c) % 10],
               '2', o.sizes [1 + (p.n % 3) + s]) AS attrs,
           c * 10 + s AS ord
    FROM seed_product p JOIN seed_template t USING (t_no) CROSS JOIN opt o,
         generate_series(0, 1) c, generate_series(0, 2) s
    WHERE t.kind = 'apparel'
    UNION ALL
    -- electronics: 2 colors x 2 storage tiers = 4 (laptops also get RAM)
    SELECT p.n, p.id, jsonb_build_object(
               '1', o.colors[1 + (p.n + c * 3) % 10],
               '3', o.storages[1 + (p.n % 3) + s])
           || CASE WHEN t.category_id = 10
                   THEN jsonb_build_object('5', o.rams[1 + s + (p.n % 2)]) ELSE '{}'::jsonb END,
           c * 10 + s
    FROM seed_product p JOIN seed_template t USING (t_no) CROSS JOIN opt o,
         generate_series(0, 1) c, generate_series(0, 1) s
    WHERE t.kind = 'electronics'
    UNION ALL
    -- material: 2 materials x 1 color = 2
    SELECT p.n, p.id, jsonb_build_object(
               '4', o.materials[1 + (p.n + m) % 6],
               '1', o.colors[1 + p.n % 10]),
           m
    FROM seed_product p JOIN seed_template t USING (t_no) CROSS JOIN opt o,
         generate_series(0, 1) m
    WHERE t.kind = 'material'
    UNION ALL
    -- volume: 2–3 sizes
    SELECT p.n, p.id, jsonb_build_object('6', o.volumes[1 + (p.n % 2) + v]), v
    FROM seed_product p JOIN seed_template t USING (t_no) CROSS JOIN opt o,
         generate_series(0, 2) v
    WHERE t.kind = 'volume' AND v < 2 + p.n % 2
    UNION ALL
    -- simple: 1–3 colors
    SELECT p.n, p.id, jsonb_build_object('1', o.colors[1 + (p.n + c * 4) % 10]), c
    FROM seed_product p JOIN seed_template t USING (t_no) CROSS JOIN opt o,
         generate_series(0, 2) c
    WHERE t.kind = 'simple' AND c < 1 + p.n % 3
)
INSERT INTO seed_variant (product_n, product_id, v_no, attrs)
SELECT n, id, row_number() OVER (PARTITION BY n ORDER BY ord), attrs
FROM matrix
ORDER BY n, ord;

INSERT INTO product_variants (id, product_id, sku, name, price, currency, status, created_at, updated_at)
SELECT v.id,
       v.product_id,
       upper(left(regexp_replace(p.brand, '[^A-Za-z]', '', 'g'), 3))
           || '-' || lpad(p.n::text, 4, '0') || '-' || lpad(v.v_no::text, 2, '0'),
       pr.name || ' - ' || (SELECT string_agg(value, ' / ' ORDER BY key)
                            FROM jsonb_each_text(v.attrs)),
       -- deterministic price within the template range, rising with variant tier, ending in .99
       floor(t.min_price + ((p.n * 37) % 100) / 100.0 * (t.max_price - t.min_price)
             + (v.v_no - 1) * (t.max_price - t.min_price) * 0.08)::numeric + 0.99,
       CASE WHEN p.n % 10 = 3 THEN 'EUR' WHEN p.n % 10 = 7 THEN 'GBP' ELSE 'USD' END,
       CASE WHEN pr.status = 1 AND (p.n + v.v_no) % 17 = 0 THEN 2   -- some inactive SKUs under active products
            ELSE pr.status
       END,
       pr.created_at,
       pr.updated_at
FROM seed_variant v
JOIN seed_product p  ON p.n = v.product_n
JOIN seed_template t ON t.t_no = p.t_no
JOIN products pr     ON pr.id = p.id;

INSERT INTO product_variant_attributes (variant_id, attribute_id, value)
SELECT v.id, a.key::bigint, a.value
FROM seed_variant v
CROSS JOIN LATERAL jsonb_each_text(v.attrs) AS a(key, value);

-- -----------------------------------------------------------------------------
-- 7. product_images
--    2 general images per product (variant_id NULL), then 1 image per variant.
-- -----------------------------------------------------------------------------
INSERT INTO product_images (product_id, variant_id, url, sort_order)
SELECT p.id, NULL,
       'https://cdn.commercehub.local/products/' || p.id || '/main-' || i || '.jpg',
       i - 1
FROM seed_product p, generate_series(1, 2) i
UNION ALL
SELECT v.product_id, v.id,
       'https://cdn.commercehub.local/products/' || v.product_id || '/variants/' || v.id || '.jpg',
       1 + v.v_no
FROM seed_variant v;

-- -----------------------------------------------------------------------------
-- 8. Sanity summary
-- -----------------------------------------------------------------------------
SELECT 'attributes' AS table_name, count(*) FROM attributes
UNION ALL SELECT 'categories',                 count(*) FROM categories
UNION ALL SELECT 'products',                   count(*) FROM products
UNION ALL SELECT 'product_categories',         count(*) FROM product_categories
UNION ALL SELECT 'product_variants',           count(*) FROM product_variants
UNION ALL SELECT 'product_variant_attributes', count(*) FROM product_variant_attributes
UNION ALL SELECT 'product_images',             count(*) FROM product_images;

COMMIT;
