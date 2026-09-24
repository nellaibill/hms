import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { defineConfig, devices } from '@playwright/test';
import dotenv from 'dotenv';

// Local, per-developer E2E settings (credentials, base URL). Never committed — see
// .env.e2e.example for the variables this suite reads.
dotenv.config({ path: path.resolve(path.dirname(fileURLToPath(import.meta.url)), '.env.e2e'), quiet: true });

const baseURL = process.env.TEST_BASE_URL || 'http://localhost:5173';
// Optional: 'chrome' or 'msedge' runs against an installed browser instead of Playwright's
// bundled Chromium (e.g. before `npx playwright install chromium` has been run).
const channel = process.env.TEST_BROWSER_CHANNEL || undefined;

export default defineConfig({
  testDir: './e2e',
  outputDir: './e2e-results',
  timeout: 30_000,
  expect: { timeout: 10_000 },
  fullyParallel: false,
  // The API limits POST /auth/login to 10 per minute per IP (RateLimiting:LoginPermitLimit),
  // so tests that really log in run serially and authenticated tests reuse one session.
  workers: 1,
  retries: process.env.CI ? 2 : 1,
  forbidOnly: !!process.env.CI,
  reporter: [['list'], ['html', { open: 'never', outputFolder: 'playwright-report' }]],
  // Starts the web app (npm run dev) when testing locally and it isn't already running; an
  // already-running dev server is reused. Skipped for remote TEST_BASE_URLs. The API must be
  // running separately.
  webServer: /^https?:\/\/localhost[:/]/.test(baseURL)
    ? { command: 'npm run dev', url: baseURL, reuseExistingServer: true, timeout: 120_000 }
    : undefined,
  use: {
    baseURL,
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
  },
  projects: [
    // Logs in once and saves the hospital session for the 'authenticated' project.
    { name: 'setup', testMatch: /auth\.setup\.ts/, use: { channel } },
    // Sign-in flow and signed-out access — always a fresh context, no saved session reused.
    {
      name: 'signed-out',
      testMatch: /auth\/(login|unauthenticated)\.spec\.ts/,
      use: { ...devices['Desktop Chrome'], channel },
    },
    // Everything else starts from the saved session (see e2e/fixtures/auth.fixture.ts).
    {
      name: 'authenticated',
      testIgnore: /auth\/(login|unauthenticated)\.spec\.ts/,
      testMatch: /.*\.spec\.ts/,
      dependencies: ['setup'],
      use: { ...devices['Desktop Chrome'], channel },
    },
  ],
});
