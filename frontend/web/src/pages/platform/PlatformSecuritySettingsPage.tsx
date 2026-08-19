import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { ApiError } from '@hms/shared';
import { ArrowLeft, ShieldCheck, ShieldOff } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { ThemeToggle } from '@/components/shell/ThemeToggle';
import { platformAuthApi } from '@/services/apiClient';

type Step = 'status' | 'setup' | 'confirmDisable';

export default function PlatformSecuritySettingsPage() {
  const navigate = useNavigate();
  const statusQuery = useQuery({ queryKey: ['platform-mfa-status'], queryFn: () => platformAuthApi.getMfaStatus() });

  const [step, setStep] = useState<Step>('status');
  const [setupSecret, setSetupSecret] = useState<{ secret: string; otpAuthUri: string } | null>(null);
  const [code, setCode] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const startSetup = async () => {
    setError(null);
    setIsSubmitting(true);
    try {
      const response = await platformAuthApi.setupMfa();
      setSetupSecret(response);
      setStep('setup');
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Unable to start MFA setup.');
    } finally {
      setIsSubmitting(false);
    }
  };

  const confirmEnable = async () => {
    if (!code.trim()) return;
    setError(null);
    setIsSubmitting(true);
    try {
      await platformAuthApi.enableMfa({ code: code.trim() });
      setCode('');
      setSetupSecret(null);
      setStep('status');
      await statusQuery.refetch();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Unable to verify the code.');
    } finally {
      setIsSubmitting(false);
    }
  };

  const confirmDisable = async () => {
    if (!code.trim()) return;
    setError(null);
    setIsSubmitting(true);
    try {
      await platformAuthApi.disableMfa({ code: code.trim() });
      setCode('');
      setStep('status');
      await statusQuery.refetch();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Unable to verify the code.');
    } finally {
      setIsSubmitting(false);
    }
  };

  const cancel = () => {
    setStep('status');
    setSetupSecret(null);
    setCode('');
    setError(null);
  };

  const mfaEnabled = statusQuery.data?.enabled ?? false;

  return (
    <div className="min-h-screen bg-muted/30">
      <header className="flex items-center justify-between border-b bg-background px-6 py-4">
        <Button variant="ghost" size="sm" onClick={() => navigate('/platform/dashboard')}>
          <ArrowLeft className="mr-2 size-4" />
          Back to dashboard
        </Button>
        <ThemeToggle />
      </header>

      <main className="mx-auto flex max-w-2xl flex-col gap-6 p-6 lg:p-8">
        <Card>
          <CardHeader>
            <CardTitle>Multi-factor authentication</CardTitle>
            <CardDescription>
              Require a code from an authenticator app (Google Authenticator, 1Password, Authy, etc.) in addition to
              your password when signing in.
            </CardDescription>
          </CardHeader>
          <CardContent className="flex flex-col gap-4">
            {error && (
              <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
                {error}
              </p>
            )}

            {step === 'status' && (
              <>
                <div className="flex items-center gap-3">
                  {mfaEnabled ? (
                    <ShieldCheck className="size-5 text-emerald-600" />
                  ) : (
                    <ShieldOff className="size-5 text-muted-foreground" />
                  )}
                  <span className="text-sm">
                    MFA is currently{' '}
                    <strong className={mfaEnabled ? 'text-emerald-600' : 'text-foreground'}>
                      {statusQuery.isPending ? '…' : mfaEnabled ? 'enabled' : 'disabled'}
                    </strong>{' '}
                    on your account.
                  </span>
                </div>

                {mfaEnabled ? (
                  <Button variant="outline" className="self-start" onClick={() => setStep('confirmDisable')}>
                    Disable MFA
                  </Button>
                ) : (
                  <Button className="self-start" onClick={startSetup} disabled={isSubmitting}>
                    {isSubmitting ? 'Starting…' : 'Set up MFA'}
                  </Button>
                )}
              </>
            )}

            {step === 'setup' && setupSecret && (
              <div className="flex flex-col gap-4">
                <p className="text-sm text-muted-foreground">
                  Add this key to your authenticator app (scanning isn't available yet — enter it manually), then
                  enter the 6-digit code it shows to confirm.
                </p>

                <div className="flex flex-col gap-1.5">
                  <Label>Manual entry key</Label>
                  <Input readOnly value={setupSecret.secret} className="font-mono" />
                </div>

                <div className="flex flex-col gap-1.5">
                  <Label>otpauth URI (some apps accept pasting this directly)</Label>
                  <Input readOnly value={setupSecret.otpAuthUri} className="font-mono text-xs" />
                </div>

                <div className="flex flex-col gap-1.5">
                  <Label htmlFor="enable-code">Verification code</Label>
                  <Input
                    id="enable-code"
                    inputMode="numeric"
                    maxLength={6}
                    placeholder="123456"
                    value={code}
                    onChange={(event) => setCode(event.target.value)}
                  />
                </div>

                <div className="flex gap-2">
                  <Button onClick={confirmEnable} disabled={isSubmitting}>
                    {isSubmitting ? 'Confirming…' : 'Confirm and enable'}
                  </Button>
                  <Button variant="outline" onClick={cancel} disabled={isSubmitting}>
                    Cancel
                  </Button>
                </div>
              </div>
            )}

            {step === 'confirmDisable' && (
              <div className="flex flex-col gap-4">
                <p className="text-sm text-muted-foreground">
                  Enter a current code from your authenticator app to confirm disabling MFA.
                </p>

                <div className="flex flex-col gap-1.5">
                  <Label htmlFor="disable-code">Verification code</Label>
                  <Input
                    id="disable-code"
                    inputMode="numeric"
                    maxLength={6}
                    placeholder="123456"
                    value={code}
                    onChange={(event) => setCode(event.target.value)}
                  />
                </div>

                <div className="flex gap-2">
                  <Button variant="destructive" onClick={confirmDisable} disabled={isSubmitting}>
                    {isSubmitting ? 'Confirming…' : 'Confirm and disable'}
                  </Button>
                  <Button variant="outline" onClick={cancel} disabled={isSubmitting}>
                    Cancel
                  </Button>
                </div>
              </div>
            )}
          </CardContent>
        </Card>
      </main>
    </div>
  );
}
