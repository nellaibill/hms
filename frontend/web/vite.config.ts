import path from 'node:path';
import react from '@vitejs/plugin-react';
import { defineConfig } from 'vite';

export default defineConfig({
  // Set by CI when deploying to GitHub Pages (a project site, served from a /<repo> subpath)
  // — left as '/' for local dev and any host that serves from the domain root.
  base: process.env.VITE_BASE_PATH || '/',
  plugins: [react()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  server: {
    port: Number(process.env.PORT) || 5173,
    warmup: {
      clientFiles: ['./src/main.tsx', './src/app/App.tsx'],
    },
    // Playwright writes traces/videos/reports inside this folder while tests run. Watching them
    // is pointless, and on Windows a still-being-written video makes the watcher throw EBUSY,
    // which kills the dev server mid-run.
    watch: {
      ignored: ['**/e2e-results/**', '**/playwright-report/**', '**/e2e/.auth/**'],
    },
  },
  optimizeDeps: {
    // @hms/shared is a linked workspace package built by tsc into a mirrored dist/
    // tree (hundreds of small files), so Vite's dep scanner otherwise skips
    // pre-bundling it and serves each file as its own request on cold start.
    //
    // The cost: because it's a `file:` link (not a real npm dependency), changing its
    // dist/ output — a fresh `npm run build` in frontend/shared, or a `git pull` that
    // touches frontend/shared — never touches package.json or the lockfile, which are
    // the only things Vite's pre-bundle cache watches to decide it's stale. Vite then
    // silently keeps serving the old bundled @hms/shared from node_modules/.vite/deps
    // until something forces a rescan — this bit us as a missing-export error right
    // after adding a new shared API method, then again as a silently-undefined method
    // (no console error, no network request, a query that just never resolves) after a
    // pull. package.json's "dev" script passes --force so this rescan always happens,
    // rather than relying on everyone remembering to `rm -rf node_modules/.vite`. Its
    // "predev" script also rebuilds frontend/shared first: --force only re-bundles whatever
    // dist/ is on disk, so a `git pull` without a shared rebuild still served old methods
    // (staging's broken Medical Information tab, regression report ENV-01).
    include: ['@hms/shared'],
  },
});
