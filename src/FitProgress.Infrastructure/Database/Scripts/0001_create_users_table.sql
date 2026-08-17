CREATE TABLE users (
    id            UUID PRIMARY KEY,
    name          VARCHAR(200) NOT NULL,
    email         VARCHAR(320) NOT NULL,
    password_hash TEXT NOT NULL,
    created_at    TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX ux_users_email_lower ON users (lower(email));
