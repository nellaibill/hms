const { spawn } = require('child_process');
const logs = require('./logs');

// Runs a one-shot command to completion, streaming its output into `channel`, prefixed
// with `[label]` so it's distinguishable when several steps share a channel (Full Setup).
//
// Deliberately no `shell: true`: git.exe and dotnet.exe are real executables that Windows'
// CreateProcess can launch directly (PATH search included) with no shell involved at all.
// `shell: true` would spawn every command through cmd.exe even when nothing about the
// command needs one — and on a VM where cmd.exe is blocked or missing, that fails outright
// with ENOENT for cmd.exe itself, not for the actual command. `npm` is the one exception
// (it's npm.cmd, a batch file) — see lib/npmCommand.js for how that's handled instead.
function runOneShot(channel, label, cmd, args, cwd, env = {}, shell = false) {
  return new Promise((resolve, reject) => {
    logs.append(channel, `[${label}] $ ${cmd} ${args.join(' ')}`, 'cmd');

    const child = spawn(cmd, args, {
      cwd,
      env: { ...process.env, ...env },
      shell,
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
