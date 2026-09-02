const git = require('./git');
const db = require('./db');
const backend = require('./backend');
const frontend = require('./frontend');
const logs = require('./logs');

const CHANNEL = 'fullsetup';

// Git -> Build -> Create DBs -> All migrations (incl. seed) -> Start Backend -> Start Frontend.
// Stops at the first failed step; each step's own detailed output still goes to its normal
// channel (git/db/backend/frontend) as well as being echoed here.
async function run(config) {
  const steps = [
    ['Git fetch', () => git.fetch(config.basePath)],
    ['Git pull', () => git.pull(config.basePath)],
    ['Build backend (dotnet build HMS.sln)', () => backend.build(config)],
    ['Install frontend dependencies (npm install)', () => frontend.install(config.basePath)],
    ['Create Platform DB', () => db.createDatabaseIfMissing(config.postgres, config.postgres.platformDatabase, 'platform-db')],
    ['Create Tenant DB', () => db.createDatabaseIfMissing(config.postgres, config.postgres.defaultDatabase, 'tenant-db')],
    ['Run migrations + seed', () => backend.migrate(config)],
    ['Start backend', () => backend.start(config)],
    ['Start frontend', () => frontend.start(config.basePath)],
  ];

  logs.append(CHANNEL, `Starting Full Setup (${steps.length} steps)...`, 'cmd');

  for (const [label, action] of steps) {
    logs.append(CHANNEL, `--- ${label} ---`, 'cmd');
    try {
      await action();
      logs.append(CHANNEL, `${label}: OK`, 'ok');
    } catch (err) {
      logs.append(CHANNEL, `${label}: FAILED — ${err.message}`, 'error');
      logs.append(CHANNEL, 'Full Setup stopped.', 'error');
      throw new Error(`Full Setup failed at step "${label}": ${err.message}`);
    }
  }

  logs.append(CHANNEL, 'Full Setup complete.', 'ok');
}

module.exports = { run };
