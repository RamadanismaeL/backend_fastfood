DROP TABLE tbUsers;
DROP TABLE tbrefreshtoken;
DROP TABLE tbIngredients;
DROP TABLE tbProducts;
DROP TABLE tbIngredientsProducts;

SELECT 1;

SELECT * FROM tbUsers;
SELECT * FROM tbRefreshToken;
SELECT * FROM tbIngredients;
SELECT * FROM tbProducts;
SELECT * FROM tbIngredientsProducts;

SELECT id, username, phone_number, roles, images, is_active, created_at, updated_at FROM tbUsers;
SELECT username FROM tbUsers WHERE username = 'ramadan';
SELECT images FROM tbUsers WHERE id = '3d789b04-b0fa-4a4c-8b07-9a4e3d1cc0c4';
SELECT username FROM tbUsers WHERE id = 'f7dda3c8-e1fc-4f0a-89de-e5443f1a8d74';
SELECT username FROM tbUsers WHERE id = (SELECT user_id FROM tbRefreshToken WHERE id = 'f7dda3c8-e1fc-4f0a-89de-e5443f1a8d74' LIMIT 1);
SELECT TO_CHAR(created_at, 'DD-MM-YYYY') AS created_date FROM tbusers;

SELECT
    COUNT(*) FILTER (WHERE is_active = TRUE)                                  AS activeCount,
    COUNT(*) FILTER (WHERE is_active = FALSE)                                 AS inactiveCount,
    COALESCE(SUM(quantity) FILTER (WHERE is_active = TRUE), 0)                AS totalActiveQty,
    COUNT(*) FILTER (WHERE expiration_status = 'Near Expiry' AND is_active)   AS nearExpiryCount,
    COUNT(*) FILTER (WHERE expiration_status = 'Expired' AND is_active)       AS expiredCount
FROM tbIngredients;

SELECT
    p.id,
    p.item_name,
    p.image_url,
    p.price,
    p.category,
    p.is_active,
    p.created_at,
    COALESCE((
        SELECT json_agg(
            json_build_object(
                'package_size', o.package_size,
                'unit_of_measure', o.unit_of_measure
            )
        )
        FROM tbIngredientsProducts o
        WHERE o.product_id = p.id
    ), '[]'::json) AS ingredients
FROM tbProducts p;

SELECT
    i.item_name AS item_name,
    ip.package_size,
    ip.unit_of_measure,
    ip.quantity
FROM tbIngredientsProducts ip
JOIN tbIngredients i ON i.id = ip.ingredient_id
WHERE ip.product_id = '798035ea-9bf0-4cee-9fcc-b6f1d42b37ab';

SELECT 1 FROM tbIngredientsProducts WHERE id = '15b23fdd-b3de-41dd-bf16-e75bc00a5b37' AND ingredient_id = '0508dc3d-f10f-4e9d-9136-c29695470d7c';

SELECT
    ip.id AS Id,
    i.item_name || ' ' || i.package_size || '' || i.unit_of_measure AS ItemName,
    ip.quantity AS Quantity
FROM tbIngredientsProducts ip
JOIN tbIngredients i ON i.id = ip.ingredient_id;

SELECT id, CONCAT_WS(' ', item_name, package_size, unit_of_measure) AS ItemName FROM tbIngredients ORDER BY ItemName ASC;

INSERT INTO tbUsers(id, username, password_hash) VALUES('333d55e0-cadc-4412-9da2-4045aa0c1510', '_ramadan', '$2a$10$BlY1qAmZZ7x4jEUUfzHb6eQbNaeZBdMHu2zfckU.0av1MhyFMrmrW');

-- 1. Produtos
INSERT INTO tbProducts (item_name, image_url, price, category, is_active, created_at) VALUES
('Bamboo Watch',        'https://example.com/img/bamboo.jpg',     65.00, 'Accessories', TRUE,  '2025-04-01 10:00:00+00'),
('Black Watch',         'https://example.com/img/black.jpg',       72.00, 'Accessories', TRUE,  '2025-04-02 11:15:00+00'),
('Blue Fitness Band',   'https://example.com/img/blue-band.jpg',   79.00, 'Fitness',     TRUE,  '2025-04-03 09:30:00+00'),
('Yoga Mat Pro',        'https://example.com/img/yoga-mat.jpg',   119.90, 'Fitness',     TRUE,  '2025-04-04 14:20:00+00'),
('Stainless Steel Bottle','https://example.com/img/bottle.jpg',     45.00, 'Accessories', FALSE, '2025-04-05 16:45:00+00');

-- 2. Ingredientes / Componentes
INSERT INTO tbIngredientsProducts (product_id, ingredient_id, package_size, unit_of_measure, quantity) VALUES
('c11c9146-3f8c-4f77-8b48-1118fbf343a4', '860ea9eb-f74f-4dbb-a319-3a90889a0a7d', 1,    'un',   1),  
('798035ea-9bf0-4cee-9fcc-b6f1d42b37ab', '860ea9eb-f74f-4dbb-a319-3a90889a0a7d', 50,   'ml',   1),  
('c11c9146-3f8c-4f77-8b48-1118fbf343a4', '2e6196dd-809b-4bb2-9848-f71751e19071', 2,    'm²',   1),  
('c11c9146-3f8c-4f77-8b48-1118fbf343a4', '291f571d-b3e1-49e6-afd6-aa2233aa91a5', 500,  'g',    0.1),
('798035ea-9bf0-4cee-9fcc-b6f1d42b37ab', '0cf73ce7-794c-4830-bd2c-e407b8b5e27d', 1,    'un',   2); 

UPDATE tbUsers SET username = 'friend', phone_number = '12345678', updated_at = NOW() WHERE id = '914e70ca-49f9-4d87-9802-58f027d6c708';

DELETE FROM tbRefreshToken WHERE id = 'c41f251c-1492-4d32-a6be-edd6d3d0bc5d';
DELETE FROM tbRefreshToken;

DELETE FROM tbusers WHERE id = '80407cfc-99a5-48b5-ac33-4975900f5415';
DELETE FROM tbUsers WHERE username = 'yes yes yes';
DELETE FROM tbIngredients WHERE id = '28b5e2d9-4611-4617-92fc-21ef7e6e9f12';

DELETE FROM tbProducts WHERE id = 'e2100cc8-1710-4762-aade-8a95c5d291f7';
-- e2100cc8-1710-4762-aade-8a95c5d291f7