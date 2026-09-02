const path = require('path');
const db = require('./db');
const runners = require('./runners');
const processes = require('./processes');

// Every path here matches the real repo layout (backend/src/HMS.Api, HMS.sln one level up).
function paths(basePath) {
  return {
    solutionDir: path.join(basePath, 'backend'),
    projectParentDir: path.join(basePath, 'backend', 'src'),
  };
}

// The env vars both `dotnet run --project HMS.Api` and the `migrate`-only run need — mirrors
// docker-compose.yml's shared x-api-env block. Jwt:SigningKey has no default in
// appsettings.json/appsettings.Development.json (JwtConfiguration.cs throws without it), and
// neither seeder's Password field has a default either (each silently skips seeding without
// one) — so this dashboard must always inject them, not just leave it to chance.
function buildEnv(config) {
  const { postgres, backend } = config;
  return {
    ASPNETCORE_ENVIRONMENT: backend.environment || 'Development',
    ConnectionStrings__Default: db.buildDotnetConnectionString(postgres, postgres.defaultDatabase),
    ConnectionStrings__Platform: db.buildDotnetConnectionString(postgres, postgres.platformDatabase),
    ConnectionStrings__PlatformAdmin: db.buildDotnetConnectionString(postgres, postgres.maintenanceDatabase),
    Jwt__SigningKey: backend.jwtSigningKey,
    SuperAdminSeed__Password: backend.superAdminSeedPassword,
    PlatformAdminSeed__Password: backend.platformAdminPassword,
    PlatformAdminSeed__Email: backend.platformAdminEmail,
    Bootstrap__SeedLegacyTenant: String(!!backend.seedLegacyTenant),
  };
}

function build(config) {
  const { solutionDir } = paths(config.basePath);
  return runners.runOneShot('backend', 'build', 'dotnet', ['build', 'HMS.sln'], solutionDir);
}

// `dotnet run --project HMS.Api -- migrate` — the same isMigrationOnlyRun path
// docs/Deployment.md documents (`dotnet HMS.Api.dll migrate`): applies every module's
// pending migrations and runs the same seed logic Development already runs on a plain
// `dotnet run`, then exits before Kestrel starts.
function migrate(config) {
  const { projectParentDir } = paths(config.basePath);
  return runners.runOneShot(
    'db',
    'migrate',
    'dotnet',
    ['run', '--project', 'HMS.Api', '--', 'migrate'],
    projectParentDir,
    buildEnv(config)
  );
}

function start(config) {
  const { projectParentDir } = paths(config.basePath);
  return processes.start('backend', {
    cmd: 'dotnet',
    args: ['run', '--project', 'HMS.Api'],
    cwd: projectParentDir,
    env: buildEnv(config),
  });
}

function stop() {
  return processes.stop('backend');
}

function restart(config) {
  const { projectParentDir } = paths(config.basePath);
  return processes.restart('backend', {
    cmd: 'dotnet',
    args: ['run', '--project', 'HMS.Api'],
    cwd: projectParentDir,
    env: buildEnv(config),
  });
}

async function healthStatus(config) {
  const processStatus = processes.describe('backend');
  let health = 'unknown';
  try {
    const controller = new AbortController();
    const timeout = setTimeout(() => controller.abort(), 2000);
    const res = await fetch(`${config.backend.baseUrl}/health`, { signal: controller.signal });
    clearTimeout(timeout);
    health = res.ok ? 'up' : `down (HTTP ${res.status})`;
  } catch (err) {
    health = 'unreachable';
  }
  return { ...processStatus, health };
}

module.exports = { paths, buildEnv, build, migrate, start, stop, restart, healthStatus };
