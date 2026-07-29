import type { CSSProperties } from 'react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { FONT_FAMILY_STACKS, FONT_SIZE_SCALE_PX, type BrandingConfig } from '../types';

interface BrandingLivePreviewProps {
  hospitalName: string;
  appTitle: string;
  logoUrl: string | null;
  fontFamily: BrandingConfig['fontFamily'];
  fontSizeScale: BrandingConfig['fontSizeScale'];
  tokens: Record<string, string>;
}

/**
 * Renders real Card/Button components inside a scoped wrapper whose inline
 * style overrides the same CSS custom properties applyBrandingTokens() writes
 * onto :root. Custom properties inherit down the DOM tree, so this reflects
 * in-progress (unsaved) form edits instantly without touching the global
 * theme the rest of the app is using — the admin sees the effect before Save.
 */
export function BrandingLivePreview({ hospitalName, appTitle, logoUrl, fontFamily, fontSizeScale, tokens }: BrandingLivePreviewProps) {
  const scopeStyle = {
    ...tokens,
    '--font-sans': FONT_FAMILY_STACKS[fontFamily],
    '--font-size-base': FONT_SIZE_SCALE_PX[fontSizeScale],
    fontFamily: 'var(--font-sans)',
    fontSize: 'var(--font-size-base)',
  } as CSSProperties;

  return (
    <div style={scopeStyle} className="overflow-hidden rounded-lg border border-border bg-background">
      <div className="flex items-center gap-3 bg-header px-4 py-3 text-header-foreground">
        {logoUrl ? (
          <img src={logoUrl} alt={hospitalName} className="h-7 w-auto rounded bg-white/90 object-contain px-1" />
        ) : (
          <span className="flex h-7 w-7 items-center justify-center rounded-md bg-white/20 text-xs font-bold">
            {hospitalName.slice(0, 1)}
          </span>
        )}
        <span className="truncate text-sm font-bold">{appTitle}</span>
      </div>

      <div className="flex">
        <div className="hidden w-36 shrink-0 border-r border-sidebar-border bg-sidebar p-3 sm:block">
          <div className="mb-2 rounded-md border-l-[3px] border-primary bg-sidebar-active px-2 py-1.5 text-xs font-medium text-sidebar-active-foreground">
            Dashboard
          </div>
          <div className="rounded-md border-l-[3px] border-transparent bg-sidebar-accent px-2 py-1.5 text-xs text-sidebar-foreground/75">
            Patients
          </div>
        </div>

        <div className="flex-1 space-y-3 p-4">
          <Card>
            <CardHeader className="p-4">
              <CardTitle className="text-sm">{hospitalName}</CardTitle>
              <CardDescription className="text-xs">Section/card header preview</CardDescription>
            </CardHeader>
            <CardContent className="flex flex-wrap gap-2 p-4 pt-0">
              <Button size="sm" variant="default">
                Primary
              </Button>
              <Button size="sm" variant="secondary">
                Secondary
              </Button>
              <Button size="sm" variant="success">
                Success
              </Button>
              <Button size="sm" variant="warning">
                Warning
              </Button>
              <Button size="sm" variant="destructive">
                Danger
              </Button>
              <Button size="sm" variant="link">
                Link style
              </Button>
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  );
}
