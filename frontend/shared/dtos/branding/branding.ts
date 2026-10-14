/** Mirrors HMS.Modules.Branding.Contracts.BrandingLogoSlots. */
export type BrandingLogoSlotDto = 'primary' | 'compact' | 'login' | 'print' | 'favicon';

/** Mirrors HMS.Modules.Branding.Contracts.LogoSlotDisplay. */
export interface LogoSlotDisplayDto {
  width: number;
  height: number;
  fit: 'contain' | 'cover' | 'fill';
}

/** Mirrors HMS.Modules.Branding.Contracts.LogoDisplaySettings. */
export interface LogoDisplaySettingsDto {
  usePrimaryAsFallback: boolean;
  slots: Partial<Record<BrandingLogoSlotDto, LogoSlotDisplayDto>>;
}

/** Mirrors HMS.Modules.Branding.Contracts.PrintPaperSizes — CSS `@page size` keywords. */
export type PrintPaperSizeDto = 'A4' | 'A3' | 'A5' | 'Letter' | 'Legal';

/** Mirrors HMS.Modules.Branding.Contracts.PrintOrientations. */
export type PrintOrientationDto = 'portrait' | 'landscape';

/** Mirrors HMS.Modules.Branding.Contracts.PrintDocumentTypes. */
export type PrintDocumentTypeDto = 'patient' | 'invoice' | 'labReport' | 'opdConsultation' | 'report';

/** Mirrors HMS.Modules.Branding.Contracts.PrintPageSetup. */
export interface PrintPageSetupDto {
  paperSize: PrintPaperSizeDto;
  orientation: PrintOrientationDto;
  marginMm: number;
}

/** Mirrors HMS.Modules.Branding.Contracts.PrintDocumentSetup — null = same as the default. */
export interface PrintDocumentSetupDto {
  paperSize: PrintPaperSizeDto | null;
  orientation: PrintOrientationDto | null;
}

/** Mirrors HMS.Modules.Branding.Contracts.PrintSettings. */
export interface PrintSettingsDto {
  default: PrintPageSetupDto;
  documents: Partial<Record<PrintDocumentTypeDto, PrintDocumentSetupDto>>;
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
  printSettings: PrintSettingsDto;
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
  /** Omit to leave the stored print settings unchanged. */
  printSettings?: PrintSettingsDto;
}
