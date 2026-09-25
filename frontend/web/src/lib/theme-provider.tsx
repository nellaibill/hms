import { createContext, useContext, useEffect, useState, type ReactNode } from 'react';
import { applyBrandingTokens } from '@/lib/apply-branding';
import { resolveBrandLogoUrl } from '@/features/branding/brandLogo';
import { useBrandingQuery } from '@/features/branding/hooks/useBrandingQuery';

type Theme = 'light' | 'dark';

interface ThemeContextValue {
  theme: Theme;
  setTheme: (theme: Theme) => void;
  toggleTheme: () => void;
}

const STORAGE_KEY = 'hms-theme';

const ThemeContext = createContext<ThemeContextValue | undefined>(undefined);

function getInitialTheme(): Theme {
  const stored = localStorage.getItem(STORAGE_KEY);
  if (stored === 'light' || stored === 'dark') return stored;
  return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
}

export function ThemeProvider({ children }: { children: ReactNode }) {
  const [theme, setThemeState] = useState<Theme>(getInitialTheme);
  // Admin-configured branding (colors, fonts, logo) — falls back to the
  // static config/branding.ts defaults while loading or if unavailable, so
  // the app is never left unstyled. See lib/apply-branding.ts.
  const { data: brandingConfig } = useBrandingQuery();

  useEffect(() => {
    const root = document.documentElement;
    root.classList.toggle('dark', theme === 'dark');
    root.setAttribute('data-theme', theme);
    localStorage.setItem(STORAGE_KEY, theme);
    applyBrandingTokens(theme, brandingConfig);
  }, [theme, brandingConfig]);

  // Configured Favicon (else the Primary logo when it's the fallback). No fallback to the
  // bundled wordmark — it's unreadable at 16px — so with nothing configured the browser keeps
  // its default tab icon, as before this setting existed.
  const faviconUrl = resolveBrandLogoUrl(brandingConfig, 'favicon', null);
  useEffect(() => {
    let link = document.querySelector<HTMLLinkElement>('link#hms-favicon');
    if (!faviconUrl) {
      link?.remove();
      return;
    }
    if (!link) {
      link = document.createElement('link');
      link.id = 'hms-favicon';
      link.rel = 'icon';
      document.head.appendChild(link);
    }
    link.href = faviconUrl;
  }, [faviconUrl]);

  const setTheme = (next: Theme) => setThemeState(next);
  const toggleTheme = () => setThemeState((prev) => (prev === 'dark' ? 'light' : 'dark'));

  return <ThemeContext.Provider value={{ theme, setTheme, toggleTheme }}>{children}</ThemeContext.Provider>;
}

export function useTheme() {
  const ctx = useContext(ThemeContext);
  if (!ctx) throw new Error('useTheme must be used within a ThemeProvider');
  return ctx;
}
