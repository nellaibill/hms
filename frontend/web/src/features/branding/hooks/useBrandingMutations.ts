import { useMutation, useQueryClient } from '@tanstack/react-query';
import { apiBrandingRepository } from '../apiBrandingRepository';
import type { BrandingConfig } from '../types';
import { brandingQueryKey } from './useBrandingQuery';

export function useUpdateBrandingMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (patch: Partial<BrandingConfig>) => apiBrandingRepository.updateBranding(patch),
    onSuccess: (config) => {
      queryClient.setQueryData(brandingQueryKey, config);
    },
  });
}

export function useUploadLogoMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (file: File) => apiBrandingRepository.uploadLogo(file),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: brandingQueryKey });
    },
  });
}

export function useResetBrandingMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => apiBrandingRepository.resetToDefaults(),
    onSuccess: (config) => {
      queryClient.setQueryData(brandingQueryKey, config);
    },
  });
}
