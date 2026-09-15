/** Mirrors HMS.Modules.Branding.Contracts.BrandingResponse. */
export interface BrandingConfigDto {
  hospitalName: string;
  appTitle: string;
  address: string | null;
  phoneNumber: string | null;
  logoUrl: string | null;
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
}
