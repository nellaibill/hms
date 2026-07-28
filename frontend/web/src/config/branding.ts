import { HeartPulse, type LucideIcon } from 'lucide-react';

/**
 * Single source of truth for brand identity. Swapping hospitals/brand color is
 * a config edit here — no component, token name, or Tailwind class changes.
 * Primary color is an HSL triple ("H S% L%", no hsl() wrapper) so it can be
 * written straight into a CSS custom property — see lib/apply-branding.ts.
 */
export interface BrandingConfig {
  hospitalName: string;
  systemName: string;
  logoIcon: LucideIcon;
  primaryColor: {
    light: string;
    dark: string;
  };
}

export const branding: BrandingConfig = {
  hospitalName: 'Lakshmi Hospitals',
  systemName: 'Hospital Management Information System',
  logoIcon: HeartPulse,
  primaryColor: {
    // Dark theme intentionally matches light's blue exactly (rather than the
    // paler tint dark UIs often use for accents) — buttons, links, and
    // rings should read as the same confident enterprise blue in both
    // themes instead of washing out to a pastel tone at night.
    light: '210 82% 45%',
    dark: '210 82% 45%',
  },
};
