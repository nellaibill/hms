# Runs inside the official postgres image's /docker-entrypoint-initdb.d hook, once, only
# against a freshly-created (empty) data volume — never on subsequent container restarts.
# POSTGRES_DB is set to the "postgres" maintenance database (matching
# ConnectionStrings:PlatformAdmin in appsettings), which the base image already creates on
# its own; this script adds the two application databases HMS.Api actually connects to:
# the legacy/default tenant (ConnectionStrings:Default) and the platform registry
# (ConnectionStrings:Platform). TENANT_DB_NAME/PLATFORM_DB_NAME come from docker-compose.yml.

create_database_if_missing() {
  local db_name="$1"
  psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname postgres <<-EOSQL
    SELECT 'CREATE DATABASE "${db_name}"'
    WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = '${db_name}')\gexec
EOSQL
}

create_database_if_missing "${TENANT_DB_NAME:?TENANT_DB_NAME is required}"
create_database_if_missing "${PLATFORM_DB_NAME:?PLATFORM_DB_NAME is required}"
