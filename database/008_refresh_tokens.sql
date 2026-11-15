-- 008_refresh_tokens.sql
-- Atomic refresh token rotation with reuse detection

BEGIN;

CREATE OR REPLACE FUNCTION rotate_refresh_token(
    p_old_hash     text,
    p_new_hash     text,
    p_new_expires  timestamptz
)
RETURNS integer
LANGUAGE plpgsql
AS $$
DECLARE
    v_token_id   bigint;
    v_user_id    integer;
    v_expires    timestamptz;
    v_revoked    timestamptz;
    v_is_active  boolean;
BEGIN
    SELECT t.id, t.user_id, t.expires_at, t.revoked_at, u.is_active
    INTO v_token_id, v_user_id, v_expires, v_revoked, v_is_active
    FROM refresh_tokens t
    JOIN users u ON u.id = t.user_id
    WHERE t.token_hash = p_old_hash
    FOR UPDATE OF t;

    IF NOT FOUND THEN
        RETURN NULL;
    END IF;

    IF v_revoked IS NOT NULL THEN
        UPDATE refresh_tokens
        SET revoked_at = now()
        WHERE user_id = v_user_id AND revoked_at IS NULL;
        RETURN NULL;
    END IF;

    IF v_expires <= now() OR NOT v_is_active THEN
        RETURN NULL;
    END IF;

    UPDATE refresh_tokens SET revoked_at = now() WHERE id = v_token_id;

    INSERT INTO refresh_tokens (user_id, token_hash, expires_at)
    VALUES (v_user_id, p_new_hash, p_new_expires);

    RETURN v_user_id;
END;
$$;

COMMIT;