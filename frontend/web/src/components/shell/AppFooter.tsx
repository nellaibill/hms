import { branding } from '@/config/branding';
import { useBrandingQuery } from '@/features/branding/hooks/useBrandingQuery';

export function AppFooter() {
  const { data: brandingConfig } = useBrandingQuery();
  const hospitalName = brandingConfig?.hospitalName ?? branding.hospitalName;

  return (
    <footer className="flex h-10 items-center justify-between border-t border-border bg-background px-6 text-xs text-muted-foreground">
      <span>
        © {new Date().getFullYear()} {hospitalName}. All rights reserved.
      </span>
      <span>HMS v0.1.0 · Application Shell (mock data)</span>
    </footer>
  );
}
