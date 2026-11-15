-- 003_stock_movements.sql
-- Append-only ledger of every stock change, plus a function to record movements safely

BEGIN;

CREATE TYPE movement_type AS ENUM ('RECEIPT', 'SALE', 'ADJUSTMENT', 'TRANSFER_IN', 'TRANSFER_OUT');

CREATE TABLE stock_movements (
    id             bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    product_id     integer NOT NULL REFERENCES products(id)   ON DELETE RESTRICT,
    warehouse_id   integer NOT NULL REFERENCES warehouses(id) ON DELETE RESTRICT,
    movement_type  movement_type NOT NULL,
    quantity_change integer NOT NULL CHECK (quantity_change <> 0),
    reference      text,
    note           text,
    created_at     timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX idx_stock_movements_product   ON stock_movements (product_id, created_at DESC);
CREATE INDEX idx_stock_movements_warehouse ON stock_movements (warehouse_id);

-- Block UPDATE and DELETE so the ledger can never be rewritten
CREATE OR REPLACE FUNCTION prevent_ledger_changes()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'stock_movements is append-only: % is not allowed', TG_OP;
END;
$$;

CREATE TRIGGER trg_stock_movements_no_change
    BEFORE UPDATE OR DELETE ON stock_movements
    FOR EACH ROW
    EXECUTE FUNCTION prevent_ledger_changes();

-- The one safe way to change stock: lock the row, check, update, log
CREATE OR REPLACE FUNCTION record_stock_movement(
    p_product_id    integer,
    p_warehouse_id  integer,
    p_type          movement_type,
    p_quantity_change integer,
    p_reference     text DEFAULT NULL,
    p_note          text DEFAULT NULL
)
RETURNS bigint
LANGUAGE plpgsql
AS $$
DECLARE
    v_current integer;
    v_movement_id bigint;
BEGIN
    -- Make sure the stock row exists
    INSERT INTO stock_levels (product_id, warehouse_id, quantity)
    VALUES (p_product_id, p_warehouse_id, 0)
    ON CONFLICT (product_id, warehouse_id) DO NOTHING;

    -- Lock this row so no one else can change it until we finish
    SELECT quantity INTO v_current
    FROM stock_levels
    WHERE product_id = p_product_id AND warehouse_id = p_warehouse_id
    FOR UPDATE;

    IF v_current + p_quantity_change < 0 THEN
        RAISE EXCEPTION 'Insufficient stock: have %, change %', v_current, p_quantity_change
            USING ERRCODE = 'check_violation';
    END IF;

    UPDATE stock_levels
    SET quantity = quantity + p_quantity_change
    WHERE product_id = p_product_id AND warehouse_id = p_warehouse_id;

    INSERT INTO stock_movements (product_id, warehouse_id, movement_type, quantity_change, reference, note)
    VALUES (p_product_id, p_warehouse_id, p_type, p_quantity_change, p_reference, p_note)
    RETURNING id INTO v_movement_id;

    RETURN v_movement_id;
END;
$$;

COMMIT;