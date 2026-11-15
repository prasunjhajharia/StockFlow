-- 005_sales_orders.sql
-- Customers, sales orders, and a function to ship them without overselling

BEGIN;

CREATE TABLE customers (
    id          integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name        text NOT NULL,
    email       text UNIQUE,
    phone       text,
    is_active   boolean NOT NULL DEFAULT true,
    created_at  timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT customers_name_not_blank CHECK (length(trim(name)) > 0)
);

CREATE TYPE sales_order_status AS ENUM ('DRAFT', 'CONFIRMED', 'SHIPPED', 'CANCELLED');

CREATE TABLE sales_orders (
    id            integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    customer_id   integer NOT NULL REFERENCES customers(id)  ON DELETE RESTRICT,
    warehouse_id  integer NOT NULL REFERENCES warehouses(id) ON DELETE RESTRICT,
    status        sales_order_status NOT NULL DEFAULT 'DRAFT',
    shipped_at    timestamptz,
    created_at    timestamptz NOT NULL DEFAULT now(),
    updated_at    timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE sales_order_lines (
    id              integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    sales_order_id  integer NOT NULL REFERENCES sales_orders(id) ON DELETE CASCADE,
    product_id      integer NOT NULL REFERENCES products(id)     ON DELETE RESTRICT,
    quantity        integer NOT NULL CHECK (quantity > 0),
    unit_price      numeric(12,2) NOT NULL CHECK (unit_price >= 0),
    UNIQUE (sales_order_id, product_id)
);

CREATE INDEX idx_so_customer ON sales_orders (customer_id);
CREATE INDEX idx_so_status   ON sales_orders (status);

CREATE TRIGGER trg_sales_orders_updated_at
    BEFORE UPDATE ON sales_orders
    FOR EACH ROW
    EXECUTE FUNCTION set_updated_at();

-- Ship an order: lock it, check status, remove stock for every line, mark SHIPPED
CREATE OR REPLACE FUNCTION ship_sales_order(p_so_id integer)
RETURNS integer   -- number of lines shipped
LANGUAGE plpgsql
AS $$
DECLARE
    v_status       sales_order_status;
    v_warehouse_id integer;
    v_line         record;
    v_count        integer := 0;
BEGIN
    SELECT status, warehouse_id INTO v_status, v_warehouse_id
    FROM sales_orders
    WHERE id = p_so_id
    FOR UPDATE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Sales order % not found', p_so_id;
    END IF;

    IF v_status <> 'CONFIRMED' THEN
        RAISE EXCEPTION 'Sales order % is %, only CONFIRMED orders can be shipped', p_so_id, v_status;
    END IF;

    FOR v_line IN
        SELECT product_id, quantity
        FROM sales_order_lines
        WHERE sales_order_id = p_so_id
        ORDER BY product_id   -- same lock order for everyone, avoids deadlocks
    LOOP
        PERFORM record_stock_movement(
            v_line.product_id, v_warehouse_id, 'SALE',
            -v_line.quantity, 'SO-' || p_so_id, NULL);
        v_count := v_count + 1;
    END LOOP;

    IF v_count = 0 THEN
        RAISE EXCEPTION 'Sales order % has no lines', p_so_id;
    END IF;

    UPDATE sales_orders
    SET status = 'SHIPPED', shipped_at = now()
    WHERE id = p_so_id;

    RETURN v_count;
END;
$$;

COMMIT;