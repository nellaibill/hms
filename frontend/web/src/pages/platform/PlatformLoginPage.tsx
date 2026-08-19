import { useState, type FormEvent } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { ApiError } from '@hms/shared';
import { Building2, ShieldCheck } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { ThemeToggle } from '@/components/shell/ThemeToggle';
import { usePlatformAuth } from '@/features/platformAuth/PlatformAuthContext';

interface LocationState {
  from?: string;
}

export default function PlatformLoginPage() {
  const { login, completeMfaLogin } = usePlatformAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  // Set once the password step passes for an MFA-enabled account — switches the form to
  // the code-entry step. See PlatformAuthContext's PlatformLoginOutcome for why login is a
  // two-step process here.
  const [mfaChallengeToken, setMfaChallengeToken] = useState<string | null>(null);
  const [mfaCode, setMfaCode] = useState('');

  const goToDestination = () => {
    const from = (location.state as LocationState | null)?.from ?? '/platform/dashboard';
    navigate(from, { replace: true });
  };

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    if (!email.trim() || !password.trim()) {
      setError('Enter an email and password to continue.');
      return;
    }

    setError(null);
    setIsSubmitting(true);
    try {
      const outcome = await login(email.trim(), password);
      if (outcome.mfaRequired) {
        setMfaChallengeToken(outcome.challengeToken);
      } else {
        goToDestination();
      }
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Unable to sign in. Please try again.');
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleMfaSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!mfaChallengeToken || !mfaCode.trim()) {
      return;
    }

    setError(null);
    setIsSubmitting(true);
    try {
      await completeMfaLogin(mfaChallengeToken, mfaCode.trim());
      goToDestination();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Unable to verify the code. Please try again.');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="relative flex min-h-screen items-center justify-center overflow-hidden bg-muted/30 px-4">
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
        {mfaChallengeToken ? (
          <>
            <CardHeader className="items-center text-center">
              <div className="mb-2 flex size-12 items-center justify-center rounded-full bg-primary/10 text-primary">
                <ShieldCheck className="size-6" />
              </div>
              <CardTitle className="mt-2">Enter your verification code</CardTitle>
              <CardDescription>Open your authenticator app and enter the current 6-digit code</CardDescription>
            </CardHeader>
            <CardContent>
              <form className="flex flex-col gap-4" onSubmit={handleMfaSubmit} noValidate>
                <div className="flex flex-col gap-1.5">
                  <Label htmlFor="mfaCode">Verification code</Label>
                  <Input
                    id="mfaCode"
                    inputMode="numeric"
                    autoComplete="one-time-code"
                    maxLength={6}
                    placeholder="123456"
                    value={mfaCode}
                    onChange={(event) => setMfaCode(event.target.value)}
                    autoFocus
                  />
                </div>

                {error && (
                  <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
                    {error}
                  </p>
                )}

                <Button type="submit" size="lg" className="mt-1" disabled={isSubmitting}>
                  {isSubmitting ? 'Verifying…' : 'Verify'}
                </Button>
                <Button
                  type="button"
                  variant="ghost"
                  disabled={isSubmitting}
                  onClick={() => {
                    setMfaChallengeToken(null);
                    setMfaCode('');
                    setError(null);
                  }}
                >
                  Back to sign in
                </Button>
              </form>
            </CardContent>
          </>
        ) : (
          <>
            <CardHeader className="items-center text-center">
              <div className="mb-2 flex size-12 items-center justify-center rounded-full bg-primary/10 text-primary">
                <Building2 className="size-6" />
              </div>
              <CardTitle className="mt-2">Platform Portal</CardTitle>
              <CardDescription>Sign in to manage hospital tenants</CardDescription>
            </CardHeader>
            <CardContent>
              <form className="flex flex-col gap-4" onSubmit={handleSubmit} noValidate>
                <div className="flex flex-col gap-1.5">
                  <Label htmlFor="email">Email</Label>
                  <Input
                    id="email"
                    type="email"
                    autoComplete="email"
                    placeholder="support@yourhms.com"
                    value={email}
                    onChange={(event) => setEmail(event.target.value)}
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
          </>
        )}
      </Card>
    </div>
  );
}
