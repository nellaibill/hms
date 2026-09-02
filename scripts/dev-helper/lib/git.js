const { execFile } = require('child_process');
const { runOneShot } = require('./runners');

function execGit(basePath, args) {
  return new Promise((resolve, reject) => {
    execFile('git', args, { cwd: basePath }, (err, stdout, stderr) => {
      if (err) {
        reject(new Error(stderr || err.message));
        return;
      }
      resolve(stdout.trim());
    });
  });
}

async function status(basePath) {
  try {
    const branch = await execGit(basePath, ['rev-parse', '--abbrev-ref', 'HEAD']);
    const porcelain = await execGit(basePath, ['status', '--porcelain']);
    const dirtyCount = porcelain ? porcelain.split(/\r?\n/).filter((l) => l.length > 0).length : 0;

    let ahead = 0;
    let behind = 0;
    let hasUpstream = true;
    try {
      const counts = await execGit(basePath, ['rev-list', '--left-right', '--count', '@{u}...HEAD']);
      const [behindStr, aheadStr] = counts.split(/\s+/);
      behind = parseInt(behindStr, 10) || 0;
      ahead = parseInt(aheadStr, 10) || 0;
    } catch {
      hasUpstream = false;
    }

    return { ok: true, branch, dirtyCount, ahead, behind, hasUpstream };
  } catch (err) {
    return { ok: false, error: err.message };
  }
}

function fetch(basePath) {
  return runOneShot('git', 'fetch', 'git', ['fetch'], basePath);
}

function pull(basePath) {
  return runOneShot('git', 'pull', 'git', ['pull'], basePath);
}

module.exports = { status, fetch, pull };
