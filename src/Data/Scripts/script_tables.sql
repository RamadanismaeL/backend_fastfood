CREATE TABLE IF NOT EXISTS tbUsers
(
    id UUID PRIMARY KEY,    
    username VARCHAR(50) UNIQUE NOT NULL,
    phone_number VARCHAR(20) NULL,
    roles VARCHAR(10) NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    images VARCHAR(255) NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NULL
);