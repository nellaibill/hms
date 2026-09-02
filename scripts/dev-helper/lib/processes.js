const { spawn, execFile } = require('child_process');
const logs = require('./logs');

// Tracks the two long-running dev processes (backend, frontend) this tool starts.
// Windows-only stop path: `taskkill /T` kills the whole process tree rooted at the PID we
// spawned, which is required here — `dotnet run` and `npm run dev` both launch a child
// process (the real apphost / vite), so killing only the parent leaves the actual server
// running and the port held. Uses execFile (spawns taskkill.exe directly, no shell) rather
// than exec (which always goes through cmd.exe) — see runners.js's comment for why this
// dashboard avoids depending on cmd.exe being spawnable at all.
const processes = new Map(); // name -> { proc, pid, status, startedAt, command }

function start(name, { cmd, args, cwd, env = {}, shell = false }) {
  const existing = processes.get(name);
  if (existing && existing.status === 'running') {
    return { started: false, reason: `${name} is already running (pid ${existing.pid})`, status: describe(name) };
  }

  logs.append(name, `$ ${cmd} ${args.join(' ')}`, 'cmd');
  logs.append(name, `(cwd: ${cwd})`, 'cmd');

  const child = spawn(cmd, args, {
    cwd,
    env: { ...process.env, ...env },
    shell,
    windowsHide: true,
  });

  const entry = {
    proc: child,
    pid: child.pid,
    status: 'running',
    startedAt: Date.now(),
    command: `${cmd} ${args.join(' ')}`,
  };
  processes.set(name, entry);

  child.stdout.on('data', (data) => logs.append(name, data.toString()));
  child.stderr.on('data', (data) => logs.append(name, data.toString(), 'error'));

  child.on('exit', (code, signal) => {
    const current = processes.get(name);
    if (current && current.pid === child.pid) {
      current.status = 'stopped';
    }
    logs.append(name, `process exited (code ${code}, signal ${signal || 'none'})`, code === 0 ? 'ok' : 'error');
  });

  return { started: true, status: describe(name) };
}

function stop(name) {
  return new Promise((resolve) => {
    const entry = processes.get(name);
    if (!entry || entry.status !== 'running') {
      resolve({ stopped: false, reason: `${name} is not running` });
      return;
    }

    logs.append(name, `stopping (taskkill /PID ${entry.pid} /T /F)...`, 'cmd');
    execFile('taskkill', ['/PID', String(entry.pid), '/T', '/F'], (err) => {
      if (err) {
        logs.append(name, `taskkill error: ${err.message}`, 'error');
      }
      entry.status = 'stopped';
      resolve({ stopped: true, status: describe(name) });
    });
  });
}

async function restart(name, startOptions) {
  await stop(name);
  await new Promise((r) => setTimeout(r, 500));
  return start(name, startOptions);
}

function describe(name) {
  const entry = processes.get(name);
  if (!entry) return { status: 'stopped', pid: null, startedAt: null, command: null };
  return { status: entry.status, pid: entry.pid, startedAt: entry.startedAt, command: entry.command };
}

module.exports = { start, stop, restart, describe };
