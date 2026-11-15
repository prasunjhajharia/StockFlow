-- 002_updated_at_trigger.sql
-- Automatically refresh updated_at whenever a row is updated

BEGIN;

CREATE OR REPLACE FUNCTION set_updated_at()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    NEW.updated_at := now();
    RETURN NEW;
END;
$$;

CREATE TRIGGER trg_products_updated_at
    BEFORE UPDATE ON products
    FOR EACH ROW
    EXECUTE FUNCTION set_updated_at();

CREATE TRIGGER trg_stock_levels_updated_at
    BEFORE UPDATE ON stock_levels
    FOR EACH ROW
    EXECUTE FUNCTION set_updated_at();

COMMIT;