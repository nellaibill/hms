const MAX_LINES = 1000;
const CHANNELS = ['git', 'db', 'backend', 'frontend', 'tenant', 'fullsetup'];

const buffers = new Map(CHANNELS.map((c) => [c, []]));

// One shared set of subscribers for every channel, not one EventSource per channel — Chrome
// caps concurrent HTTP/1.1 connections per origin at 6, and this dashboard has 6 log panels,
// so per-channel streams would permanently consume every connection slot and starve every
// other fetch on the page (status polling, button clicks, Settings) once all 6 connect.
const subscribers = new Set();

function append(channel, text, level = 'info') {
  if (!buffers.has(channel)) buffers.set(channel, []);

  const lines = String(text).split(/\r?\n/).filter((l) => l.length > 0);
  const buffer = buffers.get(channel);
  for (const line of lines) {
    const entry = { channel, ts: Date.now(), line, level };
    buffer.push(entry);
    if (buffer.length > MAX_LINES) buffer.shift();
    broadcast(entry);
  }
}

function broadcast(entry) {
  const payload = `data: ${JSON.stringify(entry)}\n\n`;
  for (const res of subscribers) {
    res.write(payload);
  }
}

function subscribeAll(res) {
  subscribers.add(res);
  res.on('close', () => subscribers.delete(res));
}

function getBuffer(channel) {
  return buffers.get(channel) || [];
}

module.exports = { append, subscribeAll, getBuffer, CHANNELS };
