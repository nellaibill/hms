import { useMutation, useQueryClient } from '@tanstack/react-query';
import { mockBrandingStore } from '../mockBrandingStore';
import type { BrandingConfig } from '../types';
import { brandingQueryKey } from './useBrandingQuery';

export function useUpdateBrandingMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (patch: Partial<BrandingConfig>) => mockBrandingStore.updateBranding(patch),
    onSuccess: (config) => {
      queryClient.setQueryData(brandingQueryKey, config);
    },
  });
}

export function useUploadLogoMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (file: File) => mockBrandingStore.uploadLogo(file),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: brandingQueryKey });
    },
  });
}

export function useResetBrandingMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => mockBrandingStore.resetToDefaults(),
    onSuccess: (config) => {
      queryClient.setQueryData(brandingQueryKey, config);
    },
  });
}
