import { createContext, useContext, useEffect, useState, type ReactNode } from 'react';
import { applyBrandingTokens } from '@/lib/apply-branding';
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

  const setTheme = (next: Theme) => setThemeState(next);
  const toggleTheme = () => setThemeState((prev) => (prev === 'dark' ? 'light' : 'dark'));

  return <ThemeContext.Provider value={{ theme, setTheme, toggleTheme }}>{children}</ThemeContext.Provider>;
}

export function useTheme() {
  const ctx = useContext(ThemeContext);
  if (!ctx) throw new Error('useTheme must be used within a ThemeProvider');
  return ctx;
}
