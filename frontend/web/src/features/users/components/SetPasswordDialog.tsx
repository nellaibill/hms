import { ApiError, setPasswordSchema, type SetPasswordFormValues, type User } from '@hms/shared';
import { zodResolver } from '@hookform/resolvers/zod';
import { useForm } from 'react-hook-form';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';

interface SetPasswordDialogProps {
  user: User;
  isSubmitting: boolean;
  apiError: ApiError | null;
  onSubmit: (password: string) => void;
  onCancel: () => void;
}

export function SetPasswordDialog({ user, isSubmitting, apiError, onSubmit, onCancel }: SetPasswordDialogProps) {
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<SetPasswordFormValues>({
    resolver: zodResolver(setPasswordSchema),
    defaultValues: { password: '', confirmPassword: '' },
  });

  const generalError = apiError?.message ?? null;

  return (
    <Dialog open onOpenChange={(open) => !open && onCancel()}>
      <DialogContent aria-labelledby="set-password-title">
        <DialogHeader>
          <DialogTitle id="set-password-title">Set password</DialogTitle>
          <DialogDescription>
            Set or reset the sign-in password for{' '}
            <strong className="text-foreground">
              {user.firstName} {user.lastName}
            </strong>{' '}
            ({user.username}).
          </DialogDescription>
        </DialogHeader>

        <form
          id="set-password-form"
          onSubmit={handleSubmit((values) => onSubmit(values.password))}
          noValidate
          className="flex flex-col gap-4"
        >
          {generalError && (
            <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
              {generalError}
            </p>
          )}

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="password">New password</Label>
            <Input id="password" type="password" autoComplete="new-password" {...register('password')} />
            {errors.password && <p className="text-sm text-destructive">{errors.password.message}</p>}
          </div>

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="confirmPassword">Confirm password</Label>
            <Input id="confirmPassword" type="password" autoComplete="new-password" {...register('confirmPassword')} />
            {errors.confirmPassword && <p className="text-sm text-destructive">{errors.confirmPassword.message}</p>}
          </div>
        </form>

        <DialogFooter>
          <Button variant="outline" onClick={onCancel} disabled={isSubmitting}>
            Cancel
          </Button>
          <Button type="submit" form="set-password-form" disabled={isSubmitting}>
            {isSubmitting ? 'Saving…' : 'Set password'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
