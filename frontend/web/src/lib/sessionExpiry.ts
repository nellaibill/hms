/**
 * A one-shot flag surviving the redirect from a protected page to a login page after the
 * global 401 handler (see services/apiClient.ts's onUnauthorized) force-logs-out an expired
 * session — the login page reads it once on mount to show "your session expired" instead of
 * the blank/default sign-in prompt, then clears it so it never resurfaces on a later,
 * unrelated visit to that page (e.g. after a normal manual logout).
 */
const FLAG_KEY = 'hms-session-expired-scope';

export type SessionScope = 'hospital' | 'platform';

export function markSessionExpired(scope: SessionScope) {
  try {
    sessionStorage.setItem(FLAG_KEY, scope);
  } catch {
    // Best-effort only — worst case the login page just doesn't show the extra message.
  }
}

/** Returns true (and clears the flag) exactly once per markSessionExpired call for this scope. */
export function consumeSessionExpiredFlag(scope: SessionScope): boolean {
  try {
    if (sessionStorage.getItem(FLAG_KEY) === scope) {
      sessionStorage.removeItem(FLAG_KEY);
      return true;
    }
  } catch {
    // Best-effort only — see markSessionExpired.
  }
  return false;
}
