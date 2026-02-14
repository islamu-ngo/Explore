-- ABOUTME: Official Cerbos PostgreSQL schema for the policy storage backend.
-- ABOUTME: Creates tables for policy storage, dependency tracking, and audit logging.
-- Source: https://github.com/cerbos/cerbos (adapted for ISLAMU Event platform)
--
-- Usage: Run this script against the PostgreSQL instance used by Cerbos PDP.
-- This is NOT the application database — Cerbos uses its own schema/database.
--
-- For docker-compose: mount as /docker-entrypoint-initdb.d/cerbos-schema.sql
-- or reference in .cerbos.yaml storage.postgres.connStr with ?search_path=cerbos

CREATE SCHEMA IF NOT EXISTS cerbos;
SET search_path TO cerbos;

-- ===== Core Policy Storage =====

CREATE TABLE IF NOT EXISTS policy (
    id                  BIGSERIAL PRIMARY KEY,
    kind                VARCHAR(128)  NOT NULL,
    name                VARCHAR(1024) NOT NULL,
    version             VARCHAR(128)  NOT NULL,
    scope               VARCHAR(512)  NOT NULL DEFAULT '',
    description         TEXT          NOT NULL DEFAULT '',
    disabled            BOOLEAN       NOT NULL DEFAULT FALSE,
    definition          JSONB         NOT NULL,
    created_at          TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at          TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT policy_unique UNIQUE (kind, name, version, scope)
);

CREATE INDEX IF NOT EXISTS idx_policy_kind ON policy (kind);
CREATE INDEX IF NOT EXISTS idx_policy_name ON policy (name);
CREATE INDEX IF NOT EXISTS idx_policy_kind_name ON policy (kind, name);

-- ===== Policy Dependencies =====
-- Tracks which policies depend on other policies (e.g., derived roles imports)

CREATE TABLE IF NOT EXISTS policy_dependency (
    policy_id           BIGINT NOT NULL REFERENCES policy(id) ON DELETE CASCADE,
    dependency_id       BIGINT NOT NULL REFERENCES policy(id) ON DELETE CASCADE,

    PRIMARY KEY (policy_id, dependency_id)
);

-- ===== Policy Ancestors =====
-- Tracks scope ancestry for hierarchical policy resolution

CREATE TABLE IF NOT EXISTS policy_ancestor (
    policy_id           BIGINT       NOT NULL REFERENCES policy(id) ON DELETE CASCADE,
    ancestor_id         BIGINT       NOT NULL REFERENCES policy(id) ON DELETE CASCADE,

    PRIMARY KEY (policy_id, ancestor_id)
);

-- ===== Policy Revision History =====
-- Audit trail for policy changes (insert/update/delete)

CREATE TABLE IF NOT EXISTS policy_revision (
    revision_id         BIGSERIAL PRIMARY KEY,
    action              VARCHAR(255)  NOT NULL,
    id                  BIGINT        NOT NULL,
    kind                VARCHAR(128)  NOT NULL,
    name                VARCHAR(1024) NOT NULL,
    version             VARCHAR(128)  NOT NULL,
    scope               VARCHAR(512)  NOT NULL DEFAULT '',
    description         TEXT          NOT NULL DEFAULT '',
    disabled            BOOLEAN       NOT NULL DEFAULT FALSE,
    definition          JSONB         NOT NULL,
    update_timestamp    TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_policy_revision_id ON policy_revision (id);

-- ===== Attribute Schema Definitions =====
-- Stores JSON schemas for validating principal/resource attributes

CREATE TABLE IF NOT EXISTS attr_schema_defs (
    id                  VARCHAR(255) PRIMARY KEY,
    definition          JSONB NOT NULL
);

-- ===== Audit Trigger =====
-- Automatically logs policy changes to the revision table

CREATE OR REPLACE FUNCTION process_policy_audit()
RETURNS TRIGGER AS $$
BEGIN
    IF (TG_OP = 'INSERT') THEN
        INSERT INTO policy_revision (action, id, kind, name, version, scope, description, disabled, definition)
        VALUES ('INSERT', NEW.id, NEW.kind, NEW.name, NEW.version, NEW.scope, NEW.description, NEW.disabled, NEW.definition);
        RETURN NEW;
    ELSIF (TG_OP = 'UPDATE') THEN
        INSERT INTO policy_revision (action, id, kind, name, version, scope, description, disabled, definition)
        VALUES ('UPDATE', NEW.id, NEW.kind, NEW.name, NEW.version, NEW.scope, NEW.description, NEW.disabled, NEW.definition);
        RETURN NEW;
    ELSIF (TG_OP = 'DELETE') THEN
        INSERT INTO policy_revision (action, id, kind, name, version, scope, description, disabled, definition)
        VALUES ('DELETE', OLD.id, OLD.kind, OLD.name, OLD.version, OLD.scope, OLD.description, OLD.disabled, OLD.definition);
        RETURN OLD;
    END IF;
    RETURN NULL;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS policy_audit ON policy;
CREATE TRIGGER policy_audit
    AFTER INSERT OR UPDATE OR DELETE ON policy
    FOR EACH ROW EXECUTE FUNCTION process_policy_audit();

-- ===== Cerbos User (optional — for connection pooling) =====
-- Uncomment if running Cerbos with a dedicated database user:
--
-- DO $$
-- BEGIN
--     IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = 'cerbos_user') THEN
--         CREATE ROLE cerbos_user WITH LOGIN PASSWORD 'cerbos_password';
--     END IF;
-- END
-- $$;
--
-- GRANT USAGE ON SCHEMA cerbos TO cerbos_user;
-- GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA cerbos TO cerbos_user;
-- GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA cerbos TO cerbos_user;
