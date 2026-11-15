-- 004_purchase_orders.sql
-- Suppliers, purchase orders, and a function to receive them into stock

BEGIN;

CREATE TABLE suppliers (
    id          integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name        text NOT NULL UNIQUE,
    email       text,
    phone       text,
    is_active   boolean NOT NULL DEFAULT true,
    created_at  timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT suppliers_name_not_blank CHECK (length(trim(name)) > 0)
);

CREATE TYPE purchase_order_status AS ENUM ('DRAFT', 'ORDERED', 'RECEIVED', 'CANCELLED');

CREATE TABLE purchase_orders (
    id            integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    supplier_id   integer NOT NULL REFERENCES suppliers(id)  ON DELETE RESTRICT,
    warehouse_id  integer NOT NULL REFERENCES warehouses(id) ON DELETE RESTRICT,
    status        purchase_order_status NOT NULL DEFAULT 'DRAFT',
    ordered_at    timestamptz,
    received_at   timestamptz,
    created_at    timestamptz NOT NULL DEFAULT now(),
    updated_at    timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE purchase_order_lines (
    id                 integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    purchase_order_id  integer NOT NULL REFERENCES purchase_orders(id) ON DELETE CASCADE,
    product_id         integer NOT NULL REFERENCES products(id)        ON DELETE RESTRICT,
    quantity           integer NOT NULL CHECK (quantity > 0),
    unit_cost          numeric(12,2) NOT NULL CHECK (unit_cost >= 0),
    UNIQUE (purchase_order_id, product_id)
);

CREATE INDEX idx_po_supplier ON purchase_orders (supplier_id);
CREATE INDEX idx_po_status   ON purchase_orders (status);

CREATE TRIGGER trg_purchase_orders_updated_at
    BEFORE UPDATE ON purchase_orders
    FOR EACH ROW
    EXECUTE FUNCTION set_updated_at();

-- Receive a PO: lock it, check status, add stock for every line, mark RECEIVED
CREATE OR REPLACE FUNCTION receive_purchase_order(p_po_id integer)
RETURNS integer   -- number of lines received
LANGUAGE plpgsql
AS $$
DECLARE
    v_status       purchase_order_status;
    v_warehouse_id integer;
    v_line         record;
    v_count        integer := 0;
BEGIN
    SELECT status, warehouse_id INTO v_status, v_warehouse_id
    FROM purchase_orders
    WHERE id = p_po_id
    FOR UPDATE;   -- stops two people receiving the same PO at once

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Purchase order % not found', p_po_id;
    END IF;

    IF v_status <> 'ORDERED' THEN
        RAISE EXCEPTION 'Purchase order % is %, only ORDERED orders can be received', p_po_id, v_status;
    END IF;

    FOR v_line IN
        SELECT product_id, quantity
        FROM purchase_order_lines
        WHERE purchase_order_id = p_po_id
        ORDER BY product_id
    LOOP
        PERFORM record_stock_movement(
            v_line.product_id, v_warehouse_id, 'RECEIPT',
            v_line.quantity, 'PO-' || p_po_id, NULL);
        v_count := v_count + 1;
    END LOOP;

    IF v_count = 0 THEN
        RAISE EXCEPTION 'Purchase order % has no lines', p_po_id;
    END IF;

    UPDATE purchase_orders
    SET status = 'RECEIVED', received_at = now()
    WHERE id = p_po_id;

    RETURN v_count;
END;
$$;

COMMIT;