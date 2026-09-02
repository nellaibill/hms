const fs = require('fs');
const path = require('path');

// On Windows, `npm` is actually `npm.cmd` — a batch file, not a real executable — and
// Windows' CreateProcess (what Node's spawn uses without `shell: true`) cannot launch batch
// files on its own; Node falls back to wrapping them through cmd.exe regardless of the
// `shell` option. That's a problem on a VM where cmd.exe is blocked/missing (see
// runners.js's comment) — git and dotnet avoid it by being real .exe files, but npm can't.
//
// The fix: run npm's own CLI script directly through node.exe instead of through npm.cmd.
// The official Node.js Windows installer (and nvm-windows) ship npm right next to node.exe,
// so this resolves cleanly for the vast majority of setups; if that layout isn't found, this
// falls back to invoking npm.cmd through a shell, which only works where cmd.exe is usable.
function resolveNpmCli() {
  const candidate = path.join(path.dirname(process.execPath), 'node_modules', 'npm', 'bin', 'npm-cli.js');
  return fs.existsSync(candidate) ? candidate : null;
}

// Returns { cmd, args, shell } for running `npm <args>` on the current platform.
function npmCommand(args) {
  if (process.platform !== 'win32') {
    return { cmd: 'npm', args, shell: false };
  }

  const npmCliPath = resolveNpmCli();
  if (npmCliPath) {
    return { cmd: process.execPath, args: [npmCliPath, ...args], shell: false };
  }

  return { cmd: 'npm.cmd', args, shell: true };
}

module.exports = { npmCommand };
