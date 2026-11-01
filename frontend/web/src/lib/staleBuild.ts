/**
 * Recovering a tab that outlived a deploy. Pages are lazy-loaded files whose names carry a
 * content hash (e.g. SettingsPage-CrPlqHXN.js), and every build renames them and removes the old
 * ones. A tab opened before the deploy still runs the previous build, so the first time it opens a
 * page it hadn't loaded yet it asks for the old file name, gets a 404, and the page can't load.
 * Reloading picks up the new build (index.html is served no-cache). It reloads once: if the same
 * failure comes back within RELOAD_GUARD_MS the file really is missing, and reloading again would
 * only loop — the route error page then offers a manual Reload instead.
 */

const RELOAD_KEY = 'hms-stale-build-reload-at';
const RELOAD_GUARD_MS = 10_000;

/** Chrome, Firefox and Safari's wording for a failed dynamic import, and Vite's for a page's CSS. */
const STALE_BUILD_MESSAGE = /Failed to fetch dynamically imported module|error loading dynamically imported module|Importing a module script failed|Unable to preload CSS/i;

let reloading = false;

export function isStaleBuildError(error: unknown): boolean {
  const message = error instanceof Error ? error.message : typeof error === 'string' ? error : '';
  return STALE_BUILD_MESSAGE.test(message);
}

/** True when an automatic reload is allowed: none in the last RELOAD_GUARD_MS. Without session
 * storage (blocked, private mode) a loop can't be ruled out, so it's never allowed. */
export function canReloadForNewBuild(): boolean {
  try {
    return Date.now() - Number(sessionStorage.getItem(RELOAD_KEY) ?? 0) >= RELOAD_GUARD_MS;
  } catch {
    return false;
  }
}

export function isReloadingForNewBuild(): boolean {
  return reloading;
}

/** Reloads the page to load the new build, unless that was already tried moments ago. Returns
 * whether a reload is under way. */
export function reloadForNewBuild(): boolean {
  if (reloading) return true;
  if (!canReloadForNewBuild()) return false;
  try {
    sessionStorage.setItem(RELOAD_KEY, String(Date.now()));
  } catch {
    return false;
  }
  reloading = true;
  window.location.reload();
  return true;
}

/** Vite fires 'vite:preloadError' when a lazy page file (or its CSS) fails to load — including
 * dynamic imports outside the router, like a library loaded on a button click. The error still
 * reaches whoever awaited the import; the reload just replaces the page moments later. */
export function installStaleBuildReload(): void {
  window.addEventListener('vite:preloadError', () => {
    reloadForNewBuild();
  });
}
