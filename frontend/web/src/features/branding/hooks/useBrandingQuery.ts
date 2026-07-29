import { useQuery } from '@tanstack/react-query';
import { apiBrandingRepository } from '../apiBrandingRepository';

export const brandingQueryKey = ['branding'] as const;

/**
 * staleTime: Infinity — branding rarely changes and every in-app navigation
 * shouldn't re-check it. The admin save flow explicitly invalidates this key
 * (useBrandingMutations.ts) to push a re-fetch + re-theme instead of polling.
 * apiBrandingRepository talks to the real HMS.Modules.Branding API and falls
 * back to the local mock store if the backend is unreachable.
 */
export function useBrandingQuery() {
  return useQuery({
    queryKey: brandingQueryKey,
    queryFn: () => apiBrandingRepository.getBranding(),
    staleTime: Infinity,
  });
}
