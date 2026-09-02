import { useState, type FormEvent } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { ApiError } from '@hms/shared';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { HospitalLogo } from '@/components/shell/HospitalLogo';
import { ThemeToggle } from '@/components/shell/ThemeToggle';
import { branding } from '@/config/branding';
import { useAuth } from '@/features/auth/AuthContext';
import { useBrandingQuery } from '@/features/branding/hooks/useBrandingQuery';
import { roleDefinitions } from '@/features/auth/roleDefinitions';
import type { Role } from '@/features/auth/types';

interface LocationState {
  from?: string;
}

// Remembers the last hospital code someone actually signed in with on this browser, so
// repeat visits (the common case) still get a helpful default without ever guessing at a
// specific tenant. Previously hardcoded to 'lhs' (the original seed tenant) — after creating
// a *different* hospital, that stale default silently resolved login against the wrong
// tenant's database unless someone remembered to clear it, producing the exact "works
// sometimes, fails other times" symptom depending on whether they noticed and retyped it.
const LAST_HOSPITAL_CODE_KEY = 'hms.lastHospitalCode';

function readLastHospitalCode(): string {
  try {
    return localStorage.getItem(LAST_HOSPITAL_CODE_KEY) ?? '';
  } catch {
    return '';
  }
}

export default function LoginPage() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const { data: brandingConfig } = useBrandingQuery();
  const appTitle = brandingConfig?.appTitle ?? branding.systemName;

  const [hospitalCode, setHospitalCode] = useState(readLastHospitalCode);
  const [role, setRole] = useState<Role>('superAdmin');
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    if (!hospitalCode.trim() || !username.trim() || !password.trim()) {
      setError('Enter your hospital code, username, and password to continue.');
      return;
    }

    setError(null);
    setIsSubmitting(true);
    try {
      const trimmedHospitalCode = hospitalCode.trim();
      await login(trimmedHospitalCode, role, username.trim(), password);
      try {
        localStorage.setItem(LAST_HOSPITAL_CODE_KEY, trimmedHospitalCode);
      } catch {
        // Best-effort convenience only — a blocked/full localStorage shouldn't fail a
        // successful login.
      }
      const from = (location.state as LocationState | null)?.from ?? '/dashboard';
      navigate(from, { replace: true });
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Unable to sign in. Please try again.');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="relative flex min-h-screen items-center justify-center overflow-hidden bg-muted/30 px-4">
      {/* Soft brand-tinted backdrop — derives from --primary, so it re-tints automatically on a brand/color change. */}
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0"
        style={{
          background:
            'radial-gradient(60% 50% at 50% 0%, hsl(var(--primary) / 0.10), transparent), radial-gradient(40% 35% at 100% 100%, hsl(var(--primary) / 0.06), transparent)',
        }}
      />

      <div className="absolute right-4 top-4">
        <ThemeToggle />
      </div>

      <Card className="relative w-full max-w-md shadow-soft-lg">
        <CardHeader className="items-center text-center">
          {/* showName=false — the app title is already shown once below as the CardDescription;
              showing it a second time next to the logo also skewed this row off-center
              (a wide fixed-width logo image + separate text block don't read as one
              centered unit the way the logo alone does). */}
          <HospitalLogo className="mb-2" showName={false} />
          <CardTitle className="mt-2">Sign in</CardTitle>
          <CardDescription>{appTitle}</CardDescription>
        </CardHeader>
        <CardContent>
          <form className="flex flex-col gap-4" onSubmit={handleSubmit} noValidate>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="hospitalCode">Hospital code</Label>
              <Input
                id="hospitalCode"
                autoComplete="organization"
                placeholder="e.g. cauvery"
                value={hospitalCode}
                onChange={(event) => setHospitalCode(event.target.value)}
              />
            </div>

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="role">Sign in as</Label>
              <Select value={role} onValueChange={(value) => setRole(value as Role)}>
                <SelectTrigger id="role">
                  <SelectValue placeholder="Select your role" />
                </SelectTrigger>
                <SelectContent>
                  {roleDefinitions.map((definition) => (
                    <SelectItem key={definition.id} value={definition.id}>
                      {definition.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              <p className="text-xs text-muted-foreground">
                {roleDefinitions.find((definition) => definition.id === role)?.description}
              </p>
            </div>

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="username">Username</Label>
              <Input
                id="username"
                autoComplete="username"
                placeholder="e.g. dr.karthikeyan"
                value={username}
                onChange={(event) => setUsername(event.target.value)}
              />
            </div>

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="password">Password</Label>
              <Input
                id="password"
                type="password"
                autoComplete="current-password"
                placeholder="••••••••"
                value={password}
                onChange={(event) => setPassword(event.target.value)}
              />
            </div>

            {error && (
              <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
                {error}
              </p>
            )}

            <Button type="submit" size="lg" className="mt-1" disabled={isSubmitting}>
              {isSubmitting ? 'Signing in…' : 'Sign in'}
            </Button>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
