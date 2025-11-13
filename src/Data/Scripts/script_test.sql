SELECT 1;

SELECT * FROM tbUsers;
SELECT * FROM tbRefreshToken;

DROP TABLE tbUsers;

SELECT id, username, phone_number, roles, images, is_active, created_at, updated_at FROM tbUsers;
SELECT username FROM tbUsers WHERE username = 'ramadan';
SELECT images FROM tbUsers WHERE id = '3d789b04-b0fa-4a4c-8b07-9a4e3d1cc0c4';
SELECT username FROM tbUsers WHERE id = 'f7dda3c8-e1fc-4f0a-89de-e5443f1a8d74';
SELECT username FROM tbUsers WHERE id = (SELECT user_id FROM tbRefreshToken WHERE id = 'f7dda3c8-e1fc-4f0a-89de-e5443f1a8d74' LIMIT 1);

DELETE FROM tbRefreshToken WHERE id = '333d55e0-cadc-4412-9da2-4045aa0c1510';
DELETE FROM tbRefreshToken;
