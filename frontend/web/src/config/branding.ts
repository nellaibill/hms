import { HeartPulse, type LucideIcon } from 'lucide-react';

/**
 * Static brand identity — the fallback name/icon used wherever a hospital's saved
 * branding isn't available. Colors live in the built-in theme
 * (features/branding/mockBrandingStore.ts's DEFAULT_TOKENS_LIGHT/DARK) instead.
 */
export interface BrandingConfig {
  hospitalName: string;
  systemName: string;
  logoIcon: LucideIcon;
}

export const branding: BrandingConfig = {
  hospitalName: 'Lakshmi Hospitals',
  systemName: 'Hospital Management Information System (HMIS)',
  logoIcon: HeartPulse,
};
