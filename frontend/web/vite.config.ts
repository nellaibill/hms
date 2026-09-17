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
  },
  optimizeDeps: {
    // @hms/shared is a linked workspace package built by tsc into a mirrored dist/
    // tree (hundreds of small files), so Vite's dep scanner otherwise skips
    // pre-bundling it and serves each file as its own request on cold start.
    include: ['@hms/shared'],
  },
});
