-- 001_create_tables.sql
-- Core tables: warehouses, products, stock_levels

BEGIN;

CREATE TABLE warehouses (
    id          integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name        text NOT NULL UNIQUE,
    location    text,
    is_active   boolean NOT NULL DEFAULT true,
    created_at  timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT warehouses_name_not_blank CHECK (length(trim(name)) > 0)
);

CREATE TABLE products (
    id             integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    sku            text NOT NULL UNIQUE,
    name           text NOT NULL,
    description    text,
    unit_price     numeric(12,2) NOT NULL CHECK (unit_price >= 0),
    reorder_level  integer NOT NULL DEFAULT 0 CHECK (reorder_level >= 0),
    is_active      boolean NOT NULL DEFAULT true,
    created_at     timestamptz NOT NULL DEFAULT now(),
    updated_at     timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT products_sku_not_blank CHECK (length(trim(sku)) > 0),
    CONSTRAINT products_name_not_blank CHECK (length(trim(name)) > 0)
);

-- How many of each product are in each warehouse
CREATE TABLE stock_levels (
    product_id    integer NOT NULL REFERENCES products(id)   ON DELETE RESTRICT,
    warehouse_id  integer NOT NULL REFERENCES warehouses(id) ON DELETE RESTRICT,
    quantity      integer NOT NULL DEFAULT 0 CHECK (quantity >= 0),
    updated_at    timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (product_id, warehouse_id)
);

-- The primary key already speeds up "by product" lookups.
-- This index speeds up "everything in this warehouse".
CREATE INDEX idx_stock_levels_warehouse ON stock_levels (warehouse_id);

COMMIT;