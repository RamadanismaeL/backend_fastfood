DROP TABLE tbUsers;
DROP TABLE tbrefreshtoken;
DROP TABLE tbIngredients;

SELECT 1;

SELECT * FROM tbUsers;
SELECT * FROM tbRefreshToken;
SELECT * FROM tbIngredients;

SELECT id, username, phone_number, roles, images, is_active, created_at, updated_at FROM tbUsers;
SELECT username FROM tbUsers WHERE username = 'ramadan';
SELECT images FROM tbUsers WHERE id = '3d789b04-b0fa-4a4c-8b07-9a4e3d1cc0c4';
SELECT username FROM tbUsers WHERE id = 'f7dda3c8-e1fc-4f0a-89de-e5443f1a8d74';
SELECT username FROM tbUsers WHERE id = (SELECT user_id FROM tbRefreshToken WHERE id = 'f7dda3c8-e1fc-4f0a-89de-e5443f1a8d74' LIMIT 1);
SELECT TO_CHAR(created_at, 'DD-MM-YYYY') AS created_date FROM tbusers;

INSERT INTO tbUsers(id, username, password_hash) VALUES('333d55e0-cadc-4412-9da2-4045aa0c1510', '_ramadan', '$2a$10$BlY1qAmZZ7x4jEUUfzHb6eQbNaeZBdMHu2zfckU.0av1MhyFMrmrW');

UPDATE tbUsers SET username = 'friend', phone_number = '12345678', updated_at = NOW() WHERE id = '914e70ca-49f9-4d87-9802-58f027d6c708';

DELETE FROM tbRefreshToken WHERE id = 'c41f251c-1492-4d32-a6be-edd6d3d0bc5d';
DELETE FROM tbRefreshToken;

DELETE FROM tbusers WHERE id = '80407cfc-99a5-48b5-ac33-4975900f5415';
DELETE FROM tbUsers WHERE username = 'yes yes yes';
DELETE FROM tbIngredients WHERE id = '28b5e2d9-4611-4617-92fc-21ef7e6e9f12';
