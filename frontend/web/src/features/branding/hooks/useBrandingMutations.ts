import { useMutation, useQueryClient } from '@tanstack/react-query';
import { apiBrandingRepository } from '../apiBrandingRepository';
import type { BrandingConfig, LogoSlot } from '../types';
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
    mutationFn: ({ file, slot }: { file: File; slot: LogoSlot }) => apiBrandingRepository.uploadLogo(file, slot),
    onSuccess: (config) => {
      queryClient.setQueryData(brandingQueryKey, config);
    },
  });
}

export function useRemoveLogoMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (slot: LogoSlot) => apiBrandingRepository.removeLogo(slot),
    onSuccess: (config) => {
      queryClient.setQueryData(brandingQueryKey, config);
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
