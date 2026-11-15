
INSERT INTO users (email, password_hash, full_name, role_id)
VALUES ('admin@example.test', 'PASTE-BCRYPT-HASH-HERE', 'First Admin',
        (SELECT id FROM roles WHERE name = 'Admin'));