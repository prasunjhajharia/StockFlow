-- seed_dev.sql
-- Sample data for development only. Run after the numbered schema files.

INSERT INTO warehouses (name, location) VALUES ('Main Warehouse', 'Aalborg');

INSERT INTO products (sku, name, unit_price, reorder_level)
VALUES ('LAP-001', 'Laptop', 899.00, 5);

INSERT INTO stock_levels (product_id, warehouse_id, quantity) VALUES (1, 1, 20);
