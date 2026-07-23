import { branding } from '@/config/branding';

export function AppFooter() {
  return (
    <footer className="flex h-10 items-center justify-between border-t border-border bg-background px-6 text-xs text-muted-foreground">
      <span>
        © {new Date().getFullYear()} {branding.hospitalName}. All rights reserved.
      </span>
      <span>HMS v0.1.0 · Application Shell (mock data)</span>
    </footer>
  );
}
