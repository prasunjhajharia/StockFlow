-- 009_user_management.sql
-- Safe role changes and activation, with "last admin" protection

BEGIN;

CREATE OR REPLACE FUNCTION change_user_role(
    p_actor_id  integer,
    p_user_id   integer,
    p_role_name text
)
RETURNS void
LANGUAGE plpgsql
AS $$
DECLARE
    v_new_role_id  integer;
    v_old_role     text;
    v_admin_count  integer;
BEGIN
    SELECT id INTO v_new_role_id FROM roles WHERE name = p_role_name;
    IF NOT FOUND THEN
        RAISE EXCEPTION 'Role % not found', p_role_name;
    END IF;

    -- Lock all active admins in a fixed order, so concurrent changes can't
    -- both remove the last two admins at once
    PERFORM 1 FROM users u JOIN roles r ON r.id = u.role_id
    WHERE r.name = 'Admin' AND u.is_active ORDER BY u.id FOR UPDATE OF u;

    SELECT r.name INTO v_old_role
    FROM users u JOIN roles r ON r.id = u.role_id
    WHERE u.id = p_user_id
    FOR UPDATE OF u;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'User % not found', p_user_id;
    END IF;

    IF v_old_role = 'Admin' AND p_role_name <> 'Admin' THEN
        IF p_actor_id = p_user_id THEN
            RAISE EXCEPTION 'You cannot remove your own Admin role';
        END IF;

        SELECT count(*) INTO v_admin_count
        FROM users u JOIN roles r ON r.id = u.role_id
        WHERE r.name = 'Admin' AND u.is_active;

        IF v_admin_count <= 1 THEN
            RAISE EXCEPTION 'Cannot remove the last active Admin';
        END IF;
    END IF;

    UPDATE users SET role_id = v_new_role_id WHERE id = p_user_id;
END;
$$;

CREATE OR REPLACE FUNCTION set_user_active(
    p_actor_id  integer,
    p_user_id   integer,
    p_active    boolean
)
RETURNS void
LANGUAGE plpgsql
AS $$
DECLARE
    v_role         text;
    v_was_active   boolean;
    v_admin_count  integer;
BEGIN
    PERFORM 1 FROM users u JOIN roles r ON r.id = u.role_id
    WHERE r.name = 'Admin' AND u.is_active ORDER BY u.id FOR UPDATE OF u;

    SELECT r.name, u.is_active INTO v_role, v_was_active
    FROM users u JOIN roles r ON r.id = u.role_id
    WHERE u.id = p_user_id
    FOR UPDATE OF u;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'User % not found', p_user_id;
    END IF;

    IF NOT p_active AND v_was_active THEN
        IF p_actor_id = p_user_id THEN
            RAISE EXCEPTION 'You cannot deactivate yourself';
        END IF;

        IF v_role = 'Admin' THEN
            SELECT count(*) INTO v_admin_count
            FROM users u JOIN roles r ON r.id = u.role_id
            WHERE r.name = 'Admin' AND u.is_active;

            IF v_admin_count <= 1 THEN
                RAISE EXCEPTION 'Cannot deactivate the last active Admin';
            END IF;
        END IF;

        -- A deactivated user must not be able to refresh their session
        UPDATE refresh_tokens SET revoked_at = now()
        WHERE user_id = p_user_id AND revoked_at IS NULL;
    END IF;

    UPDATE users SET is_active = p_active WHERE id = p_user_id;
END;
$$;

COMMIT;