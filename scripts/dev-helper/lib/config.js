const fs = require('fs');
const path = require('path');

const EXAMPLE_PATH = path.join(__dirname, '..', 'config.example.json');
const LOCAL_PATH = path.join(__dirname, '..', 'config.local.json');

function deepMerge(base, override) {
  const result = { ...base };
  for (const key of Object.keys(override || {})) {
    const value = override[key];
    if (value && typeof value === 'object' && !Array.isArray(value) && base[key] && typeof base[key] === 'object') {
      result[key] = deepMerge(base[key], value);
    } else if (value !== undefined) {
      result[key] = value;
    }
  }
  return result;
}

function defaults() {
  return JSON.parse(fs.readFileSync(EXAMPLE_PATH, 'utf8'));
}

function load() {
  const base = defaults();
  if (fs.existsSync(LOCAL_PATH)) {
    try {
      const local = JSON.parse(fs.readFileSync(LOCAL_PATH, 'utf8'));
      return deepMerge(base, local);
    } catch (err) {
      console.error(`Could not parse ${LOCAL_PATH}, falling back to defaults:`, err.message);
    }
  }
  return base;
}

function save(config) {
  fs.writeFileSync(LOCAL_PATH, JSON.stringify(config, null, 2), 'utf8');
  return config;
}

function update(partial) {
  const current = load();
  const merged = deepMerge(current, partial);
  return save(merged);
}

// Parses a simple KEY=VALUE .env file (no export/quoting logic beyond stripping
// surrounding quotes) — matches the format of this repo's own .env/.env.example.
function parseEnvFile(filePath) {
  const text = fs.readFileSync(filePath, 'utf8');
  const values = {};
  for (const rawLine of text.split(/\r?\n/)) {
    const line = rawLine.trim();
    if (!line || line.startsWith('#')) continue;
    const eq = line.indexOf('=');
    if (eq === -1) continue;
    const key = line.slice(0, eq).trim();
    let value = line.slice(eq + 1).trim();
    if ((value.startsWith('"') && value.endsWith('"')) || (value.startsWith("'") && value.endsWith("'"))) {
      value = value.slice(1, -1);
    }
    values[key] = value;
  }
  return values;
}

// Imports the parts of basePath\.env that are safe to reuse for native (non-Docker) dev.
// Deliberately skips POSTGRES_PORT: that's Docker's remapped host port (5433, to avoid
// clashing with a locally-installed Postgres) — native Postgres listens on 5432, which is
// what appsettings.Development.json already assumes.
function importFromEnvFile(basePath) {
  const envPath = path.join(basePath, '.env');
  if (!fs.existsSync(envPath)) {
    return { imported: false, reason: `No .env file found at ${envPath}` };
  }
  const values = parseEnvFile(envPath);
  const partial = { postgres: {}, backend: {} };
  if (values.POSTGRES_USER) partial.postgres.user = values.POSTGRES_USER;
  if (values.POSTGRES_PASSWORD) partial.postgres.password = values.POSTGRES_PASSWORD;
  if (values.PLATFORM_DB_NAME) partial.postgres.platformDatabase = values.PLATFORM_DB_NAME;
  if (values.TENANT_DB_NAME) partial.postgres.defaultDatabaseDockerName = values.TENANT_DB_NAME;
  if (values.JWT_SIGNING_KEY) partial.backend.jwtSigningKey = values.JWT_SIGNING_KEY;
  if (values.SUPER_ADMIN_PASSWORD) partial.backend.superAdminSeedPassword = values.SUPER_ADMIN_PASSWORD;
  if (values.PLATFORM_ADMIN_PASSWORD) partial.backend.platformAdminPassword = values.PLATFORM_ADMIN_PASSWORD;
  if (values.PLATFORM_ADMIN_EMAIL) partial.backend.platformAdminEmail = values.PLATFORM_ADMIN_EMAIL;

  const updated = update(partial);
  return { imported: true, updated, note: 'POSTGRES_PORT was not imported — that is Docker\'s remapped port, native Postgres uses 5432.' };
}

module.exports = { load, save, update, defaults, importFromEnvFile, LOCAL_PATH };
