-- 006_reports.sql
-- Reporting views and a function. They only read data, they never change it.

BEGIN;

-- 1. Low stock: products whose total quantity is at or below the reorder level.
--    LEFT JOIN keeps products that have no stock row at all (quantity 0).
CREATE OR REPLACE VIEW v_low_stock AS
SELECT p.id AS product_id,
       p.sku,
       p.name AS product_name,
       p.reorder_level,
       COALESCE(SUM(s.quantity), 0)::integer AS total_quantity
FROM products p
LEFT JOIN stock_levels s ON s.product_id = p.id
WHERE p.is_active
GROUP BY p.id, p.sku, p.name, p.reorder_level
HAVING COALESCE(SUM(s.quantity), 0) <= p.reorder_level;

-- 2. Stock value per warehouse (units x current unit price)
CREATE OR REPLACE VIEW v_stock_value AS
SELECT w.id AS warehouse_id,
       w.name AS warehouse_name,
       COALESCE(SUM(s.quantity), 0)::integer AS total_units,
       COALESCE(SUM(s.quantity * p.unit_price), 0)::numeric(14,2) AS total_value
FROM warehouses w
LEFT JOIN stock_levels s ON s.warehouse_id = w.id
LEFT JOIN products p     ON p.id = s.product_id
GROUP BY w.id, w.name;

-- 3. Sales summary: per month and product, only SHIPPED orders.
--    CTE = step 1 (group). Window functions = step 2 (rank, running total).
CREATE OR REPLACE FUNCTION sales_summary(p_from date, p_to date)
RETURNS TABLE (
    month           date,
    product_id      integer,
    sku             text,
    product_name    text,
    units_sold      bigint,
    revenue         numeric,
    rank_in_month   bigint,
    running_revenue numeric
)
LANGUAGE sql
STABLE
AS $$
    WITH monthly AS (
        SELECT date_trunc('month', so.shipped_at)::date AS month,
               l.product_id,
               SUM(l.quantity)                 AS units_sold,
               SUM(l.quantity * l.unit_price)  AS revenue
        FROM sales_orders so
        JOIN sales_order_lines l ON l.sales_order_id = so.id
        WHERE so.status = 'SHIPPED'
          AND so.shipped_at >= p_from
          AND so.shipped_at <  p_to + 1
        GROUP BY 1, 2
    )
    SELECT m.month,
           m.product_id,
           p.sku,
           p.name,
           m.units_sold,
           m.revenue,
           RANK() OVER (PARTITION BY m.month ORDER BY m.revenue DESC) AS rank_in_month,
           SUM(m.revenue) OVER (PARTITION BY m.product_id ORDER BY m.month) AS running_revenue
    FROM monthly m
    JOIN products p ON p.id = m.product_id
    ORDER BY m.month, rank_in_month;
$$;

COMMIT;
