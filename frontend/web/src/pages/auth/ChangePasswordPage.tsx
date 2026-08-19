import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { ApiError, changePasswordSchema, type ChangePasswordFormValues } from '@hms/shared';
import { zodResolver } from '@hookform/resolvers/zod';
import { useForm } from 'react-hook-form';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { HospitalLogo } from '@/components/shell/HospitalLogo';
import { ThemeToggle } from '@/components/shell/ThemeToggle';
import { useAuth } from '@/features/auth/AuthContext';
import { authApi } from '@/services/apiClient';

/**
 * Forced interstitial for a user whose current password was chosen by someone else (see
 * AuthUser.mustChangePassword's own doc comment). ProtectedRoute redirects here and blocks
 * every other route until this succeeds — the only way out is a successful submit.
 */
export default function ChangePasswordPage() {
  const { clearMustChangePassword, logout } = useAuth();
  const navigate = useNavigate();
  const [apiError, setApiError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<ChangePasswordFormValues>({
    resolver: zodResolver(changePasswordSchema),
    defaultValues: { currentPassword: '', newPassword: '', confirmNewPassword: '' },
  });

  const onSubmit = async (values: ChangePasswordFormValues) => {
    setApiError(null);
    setIsSubmitting(true);
    try {
      await authApi.changePassword({ currentPassword: values.currentPassword, newPassword: values.newPassword });
      clearMustChangePassword();
      navigate('/dashboard', { replace: true });
    } catch (err) {
      setApiError(err instanceof ApiError ? err.message : 'Unable to change password. Please try again.');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="relative flex min-h-screen items-center justify-center overflow-hidden bg-muted/30 px-4">
      <div className="absolute right-4 top-4">
        <ThemeToggle />
      </div>

      <Card className="relative w-full max-w-md shadow-soft-lg">
        <CardHeader className="items-center text-center">
          <HospitalLogo className="mb-2" showName={false} />
          <CardTitle className="mt-2">Change your password</CardTitle>
          <CardDescription>
            For security, you must set a new password before continuing — the one you signed in with was chosen by
            someone else.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <form className="flex flex-col gap-4" onSubmit={handleSubmit(onSubmit)} noValidate>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="currentPassword">Current password</Label>
              <Input
                id="currentPassword"
                type="password"
                autoComplete="current-password"
                {...register('currentPassword')}
              />
              {errors.currentPassword && <p className="text-sm text-destructive">{errors.currentPassword.message}</p>}
            </div>

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="newPassword">New password</Label>
              <Input id="newPassword" type="password" autoComplete="new-password" {...register('newPassword')} />
              {errors.newPassword && <p className="text-sm text-destructive">{errors.newPassword.message}</p>}
            </div>

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="confirmNewPassword">Confirm new password</Label>
              <Input
                id="confirmNewPassword"
                type="password"
                autoComplete="new-password"
                {...register('confirmNewPassword')}
              />
              {errors.confirmNewPassword && (
                <p className="text-sm text-destructive">{errors.confirmNewPassword.message}</p>
              )}
            </div>

            {apiError && (
              <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
                {apiError}
              </p>
            )}

            <Button type="submit" size="lg" className="mt-1" disabled={isSubmitting}>
              {isSubmitting ? 'Changing password…' : 'Change password'}
            </Button>
            <Button type="button" variant="ghost" onClick={logout} disabled={isSubmitting}>
              Sign out instead
            </Button>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
