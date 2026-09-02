const path = require('path');
const runners = require('./runners');
const processes = require('./processes');

function paths(basePath) {
  return {
    workspaceRootDir: path.join(basePath, 'frontend'),
    webDir: path.join(basePath, 'frontend', 'web'),
  };
}

// `npm install` at the workspace root (frontend/), not frontend/web — package.json there
// declares the "web" + "shared" npm workspaces, and @hms/web depends on @hms/shared via
// "file:../shared", so installing from the root is what actually links them.
function install(basePath) {
  const { workspaceRootDir } = paths(basePath);
  return runners.runOneShot('frontend', 'npm install', 'npm', ['install'], workspaceRootDir);
}

function startOptions(basePath) {
  const { webDir } = paths(basePath);
  return {
    cmd: 'npm',
    args: ['run', 'dev', '--', '--host', '0.0.0.0'],
    cwd: webDir,
    env: {},
  };
}

function start(basePath) {
  return processes.start('frontend', startOptions(basePath));
}

function stop() {
  return processes.stop('frontend');
}

function restart(basePath) {
  return processes.restart('frontend', startOptions(basePath));
}

async function status(config) {
  const processStatus = processes.describe('frontend');
  let reachable = 'unknown';
  try {
    const controller = new AbortController();
    const timeout = setTimeout(() => controller.abort(), 2000);
    const res = await fetch(config.frontend.url, { signal: controller.signal });
    clearTimeout(timeout);
    reachable = res.ok ? 'up' : `down (HTTP ${res.status})`;
  } catch (err) {
    reachable = 'unreachable';
  }
  return { ...processStatus, reachable };
}

module.exports = { paths, install, start, stop, restart, status };
