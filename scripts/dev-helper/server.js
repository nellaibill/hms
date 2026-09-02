const express = require('express');
const path = require('path');

const configStore = require('./lib/config');
const logs = require('./lib/logs');
const git = require('./lib/git');
const db = require('./lib/db');
const backend = require('./lib/backend');
const frontend = require('./lib/frontend');
const tenant = require('./lib/tenant');
const fullSetup = require('./lib/fullSetup');

const app = express();
app.use(express.json());
app.use(express.static(path.join(__dirname, 'public')));

function handle(fn) {
  return async (req, res) => {
    try {
      const result = await fn(req);
      res.json({ ok: true, ...result });
    } catch (err) {
      res.status(400).json({ ok: false, error: err.message });
    }
  };
}

// ---- Config ----

app.get('/api/config', (req, res) => {
  res.json(configStore.load());
});

app.post('/api/config', handle(async (req) => {
  const updated = configStore.update(req.body);
  return { config: updated };
}));

app.post('/api/config/import-env', handle(async () => {
  const config = configStore.load();
  const result = configStore.importFromEnvFile(config.basePath);
  return result;
}));

// ---- Git ----

app.get('/api/git/status', handle(async () => {
  const config = configStore.load();
  return { status: await git.status(config.basePath) };
}));

app.post('/api/git/fetch', handle(async () => {
  const config = configStore.load();
  await git.fetch(config.basePath);
  return {};
}));

app.post('/api/git/pull', handle(async () => {
  const config = configStore.load();
  await git.pull(config.basePath);
  return {};
}));

// ---- Database ----

app.get('/api/db/status', handle(async () => {
  const config = configStore.load();
  return { status: await db.status(config.postgres) };
}));

app.post('/api/db/create-platform', handle(async () => {
  const config = configStore.load();
  return await db.createDatabaseIfMissing(config.postgres, config.postgres.platformDatabase, 'platform-db');
}));

app.post('/api/db/create-tenant', handle(async () => {
  const config = configStore.load();
  return await db.createDatabaseIfMissing(config.postgres, config.postgres.defaultDatabase, 'tenant-db');
}));

app.post('/api/db/migrate', handle(async () => {
  const config = configStore.load();
  await backend.migrate(config);
  return {};
}));

app.post('/api/db/seed', handle(async () => {
  // Seeding isn't a separate command in this codebase — Program.cs runs the seed logic as
  // part of the same migrate step (PlatformModule.SeedAsync / IdentityModule.SeedAsync).
  // Exposed as its own idempotent button because that's how the dashboard is organized.
  const config = configStore.load();
  await backend.migrate(config);
  return {};
}));

// ---- Backend ----

app.get('/api/backend/status', handle(async () => {
  const config = configStore.load();
  return { status: await backend.healthStatus(config) };
}));

app.post('/api/backend/build', handle(async () => {
  const config = configStore.load();
  await backend.build(config);
  return {};
}));

app.post('/api/backend/start', handle(async () => {
  const config = configStore.load();
  return backend.start(config);
}));

app.post('/api/backend/stop', handle(async () => {
  return await backend.stop();
}));

app.post('/api/backend/restart', handle(async () => {
  const config = configStore.load();
  return await backend.restart(config);
}));

// ---- Frontend ----

app.get('/api/frontend/status', handle(async () => {
  const config = configStore.load();
  return { status: await frontend.status(config) };
}));

app.post('/api/frontend/install', handle(async () => {
  const config = configStore.load();
  await frontend.install(config.basePath);
  return {};
}));

app.post('/api/frontend/start', handle(async () => {
  const config = configStore.load();
  return frontend.start(config.basePath);
}));

app.post('/api/frontend/stop', handle(async () => {
  return await frontend.stop();
}));

app.post('/api/frontend/restart', handle(async () => {
  const config = configStore.load();
  return await frontend.restart(config.basePath);
}));

// ---- Tenant seed ----

app.post('/api/tenant/create', handle(async (req) => {
  const config = configStore.load();
  const created = await tenant.createHospital(config, req.body || {});
  return { hospital: created };
}));

// ---- Full setup ----

app.post('/api/full-setup', handle(async () => {
  const config = configStore.load();
  // Runs in the background; progress streams over the 'fullsetup' log channel. Responding
  // immediately (rather than awaiting) keeps the request from timing out across a
  // multi-minute pipeline.
  fullSetup.run(config).catch(() => {}); // failures are already logged to the channel
  return { started: true };
}));

// ---- Logs (SSE) ----

// Must come before the '/api/logs/:channel' route below — Express matches routes in
// registration order, and ':channel' would otherwise greedily match "stream" as a channel
// name, making this SSE route unreachable.
// One shared stream for every channel (see lib/logs.js for why) — each event carries its
// own `channel` field so the client can route it to the right log panel.
app.get('/api/logs/stream', (req, res) => {
  res.set({
    'Content-Type': 'text/event-stream',
    'Cache-Control': 'no-cache',
    Connection: 'keep-alive',
  });
  res.flushHeaders();
  logs.subscribeAll(res);
});

app.get('/api/logs/:channel', (req, res) => {
  res.json(logs.getBuffer(req.params.channel));
});

// Dashboard listens on 127.0.0.1 only — unlike the HMS frontend/backend it manages, this
// control surface should never be reachable from another machine on the network.
const config = configStore.load();
const port = config.dashboardPort || 4500;
app.listen(port, '127.0.0.1', () => {
  console.log(`HMS Dev Helper running at http://localhost:${port}`);
});
