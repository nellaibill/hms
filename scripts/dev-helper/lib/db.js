const { Client } = require('pg');
const logs = require('./logs');

// Postgres identifiers are capped at 63 bytes (NAMEDATALEN - 1) — same bound
// TenantProvisioningService.cs enforces server-side for tenant database names.
const SAFE_IDENTIFIER = /^[a-zA-Z_][a-zA-Z0-9_]{0,62}$/;

function pgUri(pg, database) {
  return `postgresql://${encodeURIComponent(pg.user)}:${encodeURIComponent(pg.password)}@${pg.host}:${pg.port}/${database}`;
}

async function withMaintenanceClient(pg, fn) {
  const client = new Client({ connectionString: pgUri(pg, pg.maintenanceDatabase) });
  await client.connect();
  try {
    return await fn(client);
  } finally {
    await client.end();
  }
}

async function databaseExists(client, name) {
  const result = await client.query('SELECT 1 FROM pg_database WHERE datname = $1', [name]);
  return result.rowCount > 0;
}

async function createDatabaseIfMissing(pg, name, label) {
  if (!SAFE_IDENTIFIER.test(name)) {
    throw new Error(`"${name}" is not a safe Postgres database name (letters, digits, underscore only).`);
  }
  logs.append('db', `[${label}] connecting to ${pg.host}:${pg.port}/${pg.maintenanceDatabase} as ${pg.user}...`, 'cmd');
  return withMaintenanceClient(pg, async (client) => {
    if (await databaseExists(client, name)) {
      logs.append('db', `[${label}] database "${name}" already exists — skipping.`, 'ok');
      return { created: false, name };
    }
    // CREATE DATABASE can't take a bind parameter for the identifier; SAFE_IDENTIFIER above
    // guards this interpolation the same way TenantProvisioningService.cs does.
    await client.query(`CREATE DATABASE "${name}"`);
    logs.append('db', `[${label}] created database "${name}".`, 'ok');
    return { created: true, name };
  });
}

async function status(pg) {
  const result = {
    serverReachable: false,
    platformDbExists: false,
    defaultDbExists: false,
    error: null,
  };
  try {
    await withMaintenanceClient(pg, async (client) => {
      result.serverReachable = true;
      result.platformDbExists = await databaseExists(client, pg.platformDatabase);
      result.defaultDbExists = await databaseExists(client, pg.defaultDatabase);
    });
  } catch (err) {
    result.error = err.message;
  }
  return result;
}

// ADO-style connection string (ConnectionStrings__* env vars) — same shape docker-compose.yml
// builds for HMS.Api, just with Host=localhost instead of Host=postgres.
function buildDotnetConnectionString(pg, database) {
  return `Host=${pg.host};Port=${pg.port};Database=${database};Username=${pg.user};Password=${pg.password}`;
}

module.exports = { createDatabaseIfMissing, status, buildDotnetConnectionString };
