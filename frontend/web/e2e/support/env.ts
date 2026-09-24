import path from 'node:path';
import { fileURLToPath } from 'node:url';

/** Saved hospital session (sessionStorage['hms-session']) produced by auth.setup.ts. */
export const AUTH_STATE_FILE = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../.auth/session.json');

export interface TestCredentials {
  hospitalCode: string;
  role: string;
  username: string;
  password: string;
}

/**
 * Reads the test account from the environment (.env.e2e locally, CI secrets in CI).
 * Fails loudly with the missing variable names rather than silently running with blanks.
 */
export function getTestCredentials(): TestCredentials {
  const required = ['TEST_HOSPITAL_CODE', 'TEST_USERNAME', 'TEST_PASSWORD'] as const;
  const missing = required.filter((name) => !process.env[name]);
  if (missing.length > 0) {
    throw new Error(
      `Missing E2E credentials: ${missing.join(', ')}. Copy frontend/web/.env.e2e.example to .env.e2e and fill it in.`,
    );
  }
  return {
    hospitalCode: process.env.TEST_HOSPITAL_CODE!,
    role: process.env.TEST_ROLE || 'Super Admin',
    username: process.env.TEST_USERNAME!,
    password: process.env.TEST_PASSWORD!,
  };
}

export const LOGIN_API_PATH = '/api/v1/auth/login';
