const { spawn } = require('child_process');
const logs = require('./logs');

// Runs a one-shot command to completion, streaming its output into `channel`, prefixed
// with `[label]` so it's distinguishable when several steps share a channel (Full Setup).
function runOneShot(channel, label, cmd, args, cwd, env = {}) {
  return new Promise((resolve, reject) => {
    logs.append(channel, `[${label}] $ ${cmd} ${args.join(' ')}`, 'cmd');

    const child = spawn(cmd, args, {
      cwd,
      env: { ...process.env, ...env },
      shell: true,
      windowsHide: true,
    });

    child.stdout.on('data', (data) => logs.append(channel, prefixLines(label, data.toString())));
    child.stderr.on('data', (data) => logs.append(channel, prefixLines(label, data.toString()), 'error'));

    child.on('error', (err) => {
      logs.append(channel, `[${label}] failed to start: ${err.message}`, 'error');
      reject(err);
    });

    child.on('close', (code) => {
      if (code === 0) {
        logs.append(channel, `[${label}] done (exit code 0)`, 'ok');
        resolve({ code });
      } else {
        logs.append(channel, `[${label}] failed (exit code ${code})`, 'error');
        reject(new Error(`${label} exited with code ${code}`));
      }
    });
  });
}

function prefixLines(label, text) {
  return text
    .split(/\r?\n/)
    .filter((l) => l.length > 0)
    .map((l) => `[${label}] ${l}`)
    .join('\n');
}

module.exports = { runOneShot };
