/** Mirrors HMS.Modules.Branding.Contracts.BrandingLogoSlots. */
export type BrandingLogoSlotDto = 'primary' | 'compact' | 'login' | 'print' | 'favicon';

/** Mirrors HMS.Modules.Branding.Contracts.LogoSlotDisplay. */
export interface LogoSlotDisplayDto {
  height: number;
  fit: 'contain' | 'scale-down';
}

/** Mirrors HMS.Modules.Branding.Contracts.LogoDisplaySettings. */
export interface LogoDisplaySettingsDto {
  usePrimaryAsFallback: boolean;
  slots: Partial<Record<BrandingLogoSlotDto, LogoSlotDisplayDto>>;
}

/** Mirrors HMS.Modules.Branding.Contracts.BrandingResponse. */
export interface BrandingConfigDto {
  hospitalName: string;
  appTitle: string;
  address: string | null;
  phoneNumber: string | null;
  logoUrl: string | null;
  compactLogoUrl: string | null;
  loginLogoUrl: string | null;
  printLogoUrl: string | null;
  faviconUrl: string | null;
  logoDisplay: LogoDisplaySettingsDto;
  fontFamily: string;
  fontSizeScale: string;
  iconSizeScale: string;
  tokensLight: Record<string, string>;
  tokensDark: Record<string, string>;
}

/** Mirrors HMS.Modules.Branding.Contracts.UpdateBrandingRequest. */
export interface UpdateBrandingRequest {
  hospitalName: string;
  appTitle: string;
  address: string | null;
  phoneNumber: string | null;
  fontFamily: string;
  fontSizeScale: string;
  iconSizeScale: string;
  tokensLight: Record<string, string>;
  tokensDark: Record<string, string>;
  /** Omit to leave the stored logo display settings unchanged. */
  logoDisplay?: LogoDisplaySettingsDto;
}
