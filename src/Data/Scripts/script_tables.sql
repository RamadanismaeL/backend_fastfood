CREATE EXTENSION IF NOT EXISTS pgcrypto;


CREATE TABLE IF NOT EXISTS tbUsers
(
    id UUID PRIMARY KEY,    
    username VARCHAR(50) UNIQUE NOT NULL,
    phone_number VARCHAR(20) NULL,
    roles VARCHAR(10) NOT NULL DEFAULT 'user',
    password_hash VARCHAR(255) NOT NULL,
    images VARCHAR(255) NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NULL
);

CREATE TABLE IF NOT EXISTS tbRefreshToken
(
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL REFERENCES tbUsers(id) ON DELETE CASCADE,
    token TEXT NOT NULL UNIQUE,
    expires_at TIMESTAMPTZ NOT NULL,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    revoked_at TIMESTAMPTZ
);

CREATE TABLE IF NOT EXISTS tbIngredients (
    id UUID PRIMARY KEY,    
    item_name VARCHAR(50) NOT NULL,    
    batch_number VARCHAR(50) NULL,
    package_size NUMERIC DEFAULT 0,    
    unit_of_measure VARCHAR(10),        
    quantity NUMERIC DEFAULT 0,
    unit_cost_price DECIMAL(10,2) DEFAULT 0.00,
    total_cost_price DECIMAL(12,2) GENERATED ALWAYS AS 
        (quantity * unit_cost_price) STORED,    
    expiration_at TIMESTAMPTZ NULL,    
    expiration_status VARCHAR(25) NULL,    
    is_active BOOLEAN NOT NULL DEFAULT TRUE,    
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,    
    updated_at TIMESTAMPTZ DEFAULT NULL,

    CONSTRAINT up_item_batch_expiry UNIQUE (item_name, expiration_at)
);

CREATE TABLE IF NOT EXISTS tbProducts (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    item_name VARCHAR(50) NOT NULL,
    image_url VARCHAR(255) NULL,    
    price DECIMAL(10,2) DEFAULT 0.00,
    category VARCHAR(50) NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS tbIngredientsProducts (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    product_id UUID NOT NULL REFERENCES tbProducts(id) ON DELETE CASCADE,
    ingredient_id UUID NOT NULL REFERENCES tbIngredients(id) ON DELETE CASCADE,
    package_size NUMERIC DEFAULT 0,
    unit_of_measure VARCHAR(10),
    quantity NUMERIC(10,3) DEFAULT 0 CHECK (quantity >= 0),
    
    -- Prevent the same ingredient being added twice to the same product
    UNIQUE(product_id, ingredient_id)
);
CREATE INDEX idx_ingredientsproducts_ingredient ON tbIngredientsProducts(ingredient_id);