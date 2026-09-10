import { useQuery } from '@tanstack/react-query';
import { apiBrandingRepository } from '../apiBrandingRepository';

export const brandingQueryKey = ['branding'] as const;

/**
 * staleTime: Infinity — branding rarely changes and every in-app navigation
 * shouldn't re-check it. Two flows explicitly invalidate this key instead of polling:
 * the admin save flow (useBrandingMutations.ts), and login/logout (AuthContext.tsx) —
 * the backend now resolves branding per-tenant, so a stale cached response from a
 * previous session/hospital in the same browser tab must be discarded on every
 * login/logout, not just on an explicit save. apiBrandingRepository talks to the real
 * HMS.Modules.Branding API and falls back to the local mock store (generic defaults)
 * when the backend is unreachable — including the pre-login screen, which never has a
 * tenant to fetch for and always gets this fallback via the API call's own 401.
 */
export function useBrandingQuery() {
  return useQuery({
    queryKey: brandingQueryKey,
    queryFn: () => apiBrandingRepository.getBranding(),
    staleTime: Infinity,
  });
}
