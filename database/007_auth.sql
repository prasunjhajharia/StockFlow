-- 007_auth.sql
-- Roles, users, refresh tokens, login history, and lockout logic

BEGIN;

CREATE TABLE roles (
    id    integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name  text NOT NULL UNIQUE
);

-- Reference data the app needs in order to work, so it lives in the schema file
INSERT INTO roles (name) VALUES ('Admin'), ('Manager'), ('Staff');

CREATE TABLE users (
    id                  integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    email               text NOT NULL,
    password_hash       text NOT NULL,
    full_name           text NOT NULL,
    role_id             integer NOT NULL REFERENCES roles(id) ON DELETE RESTRICT,
    is_active           boolean NOT NULL DEFAULT true,
    failed_login_count  integer NOT NULL DEFAULT 0,
    locked_until        timestamptz,
    last_login_at       timestamptz,
    created_at          timestamptz NOT NULL DEFAULT now(),
    updated_at          timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT users_email_lowercase CHECK (email = lower(email)),
    CONSTRAINT users_email_not_blank CHECK (length(trim(email)) > 0)
);

CREATE UNIQUE INDEX uq_users_email ON users (email);

CREATE TRIGGER trg_users_updated_at
    BEFORE UPDATE ON users
    FOR EACH ROW
    EXECUTE FUNCTION set_updated_at();

CREATE TABLE refresh_tokens (
    id          bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    user_id     integer NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    token_hash  text NOT NULL UNIQUE,
    expires_at  timestamptz NOT NULL,
    revoked_at  timestamptz,
    created_at  timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX idx_refresh_tokens_user ON refresh_tokens (user_id);

CREATE TABLE login_history (
    id               bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    user_id          integer REFERENCES users(id) ON DELETE SET NULL,
    email_attempted  text NOT NULL,
    succeeded        boolean NOT NULL,
    reason           text NOT NULL,
    ip_address       text,
    user_agent       text,
    created_at       timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX idx_login_history_user    ON login_history (user_id, created_at DESC);
CREATE INDEX idx_login_history_created ON login_history (created_at DESC);

-- Record a login attempt and update the lockout state, atomically.
-- The C# code checks locked_until BEFORE verifying the password,
-- then calls this with the result.
CREATE OR REPLACE FUNCTION record_login_attempt(
    p_email       text,
    p_succeeded   boolean,
    p_ip          text DEFAULT NULL,
    p_user_agent  text DEFAULT NULL
)
RETURNS void
LANGUAGE plpgsql
AS $$
DECLARE
    v_user_id  integer;
    v_failed   integer;
    c_max_failures CONSTANT integer := 5;
    c_lock_minutes CONSTANT integer := 15;
BEGIN
    -- Lock the user row so two parallel attempts can't miscount
    SELECT id, failed_login_count INTO v_user_id, v_failed
    FROM users
    WHERE email = lower(trim(p_email))
    FOR UPDATE;

    IF NOT FOUND THEN
        INSERT INTO login_history (user_id, email_attempted, succeeded, reason, ip_address, user_agent)
        VALUES (NULL, lower(trim(p_email)), false, 'UNKNOWN_USER', p_ip, p_user_agent);
        RETURN;
    END IF;

    IF p_succeeded THEN
        UPDATE users
        SET failed_login_count = 0, locked_until = NULL, last_login_at = now()
        WHERE id = v_user_id;

        INSERT INTO login_history (user_id, email_attempted, succeeded, reason, ip_address, user_agent)
        VALUES (v_user_id, lower(trim(p_email)), true, 'OK', p_ip, p_user_agent);
    ELSE
        v_failed := v_failed + 1;

        IF v_failed >= c_max_failures THEN
            UPDATE users
            SET failed_login_count = 0,
                locked_until = now() + make_interval(mins => c_lock_minutes)
            WHERE id = v_user_id;

            INSERT INTO login_history (user_id, email_attempted, succeeded, reason, ip_address, user_agent)
            VALUES (v_user_id, lower(trim(p_email)), false, 'LOCKED_OUT', p_ip, p_user_agent);
        ELSE
            UPDATE users SET failed_login_count = v_failed WHERE id = v_user_id;

            INSERT INTO login_history (user_id, email_attempted, succeeded, reason, ip_address, user_agent)
            VALUES (v_user_id, lower(trim(p_email)), false, 'BAD_PASSWORD', p_ip, p_user_agent);
        END IF;
    END IF;
END;
$$;

COMMIT;