
INSERT INTO warehouses (name, location) VALUES ('Main Warehouse', 'Aalborg');

INSERT INTO products (sku, name, unit_price, reorder_level)
VALUES ('LAP-001', 'Laptop', 899.00, 5);

INSERT INTO suppliers (name, email) VALUES ('Acme Supplies', 'sales@acme.test');
INSERT INTO customers (name, email) VALUES ('Jens Hansen', 'jens@example.test');

SELECT record_stock_movement(1, 1, 'RECEIPT'::movement_type, 20, 'SEED', 'Initial stock');