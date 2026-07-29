import { useQuery } from '@tanstack/react-query';
import { mockBrandingStore } from '../mockBrandingStore';

export const brandingQueryKey = ['branding'] as const;

/**
 * staleTime: Infinity — branding rarely changes and every in-app navigation
 * shouldn't re-check it. The admin save flow explicitly invalidates this key
 * (useBrandingMutations.ts) to push a re-fetch + re-theme instead of polling.
 */
export function useBrandingQuery() {
  return useQuery({
    queryKey: brandingQueryKey,
    queryFn: () => mockBrandingStore.getBranding(),
    staleTime: Infinity,
  });
}
