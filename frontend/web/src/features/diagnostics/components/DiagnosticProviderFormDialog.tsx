import { ApiError, createDiagnosticProviderSchema, updateDiagnosticProviderSchema, type DiagnosticProvider, type DiagnosticProviderFormValues } from '@hms/shared';
import { zodResolver } from '@hookform/resolvers/zod';
import { Controller, useForm } from 'react-hook-form';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Switch } from '@/components/ui/switch';
import { useCreateDiagnosticProviderMutation, useUpdateDiagnosticProviderMutation } from '../hooks/useDiagnosticProviderMutations';

interface DiagnosticProviderFormDialogProps {
  mode: 'create' | 'edit';
  /** Required when mode is 'edit'. */
  provider?: DiagnosticProvider;
  onClose: () => void;
}

/** DiagnosticProvider ("External Lab") is tiny (3 real fields) — a single list page with this
 * create/edit modal is enough, rather than dedicated create/edit/view route pages. Mirrors
 * LeaveTypeFormDialog/DiagnosticCategoryFormDialog. */
export function DiagnosticProviderFormDialog({ mode, provider, onClose }: DiagnosticProviderFormDialogProps) {
  const createMutation = useCreateDiagnosticProviderMutation();
  const updateMutation = useUpdateDiagnosticProviderMutation();
  const mutation = mode === 'create' ? createMutation : updateMutation;

  const {
    register,
    control,
    handleSubmit,
    formState: { errors },
  } = useForm<DiagnosticProviderFormValues>({
    resolver: zodResolver(mode === 'create' ? createDiagnosticProviderSchema : updateDiagnosticProviderSchema),
    defaultValues: {
      code: provider?.code ?? '',
      name: provider?.name ?? '',
      contactDetails: provider?.contactDetails ?? '',
      isActive: provider?.isActive ?? true,
    },
  });

  function onSubmit(values: DiagnosticProviderFormValues) {
    const contactDetails = values.contactDetails || undefined;

    if (mode === 'create') {
      createMutation.mutate({ code: values.code, name: values.name, contactDetails, isActive: values.isActive }, { onSuccess: onClose });
    } else if (provider) {
      updateMutation.mutate(
        { id: provider.id, request: { code: provider.code, name: values.name, contactDetails, isActive: values.isActive } },
        { onSuccess: onClose },
      );
    }
  }

  const apiError = mutation.error instanceof ApiError ? mutation.error : null;

  return (
    <Dialog open onOpenChange={(open) => !open && onClose()}>
      <DialogContent aria-labelledby="diagnostic-provider-form-title">
        <DialogHeader>
          <DialogTitle id="diagnostic-provider-form-title">{mode === 'create' ? 'New External Lab' : 'Edit External Lab'}</DialogTitle>
        </DialogHeader>

        <form onSubmit={handleSubmit(onSubmit)} noValidate className="flex flex-col gap-4">
          {apiError && !apiError.validationErrors && (
            <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
              {apiError.message}
            </p>
          )}

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="dp-code">Code</Label>
            <Input id="dp-code" disabled={mode === 'edit'} {...register('code')} />
            {mode === 'edit' && <p className="text-xs text-muted-foreground">Code can't be changed after creation.</p>}
            {errors.code && <p className="text-sm text-destructive">{errors.code.message}</p>}
          </div>

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="dp-name">Name</Label>
            <Input id="dp-name" {...register('name')} />
            {errors.name && <p className="text-sm text-destructive">{errors.name.message}</p>}
          </div>

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="dp-contactDetails">Contact Details</Label>
            <Input id="dp-contactDetails" {...register('contactDetails')} />
            {errors.contactDetails && <p className="text-sm text-destructive">{errors.contactDetails.message}</p>}
          </div>

          <div className="flex items-center justify-between rounded-md border border-border px-3 py-2.5">
            <Label htmlFor="dp-isActive" className="cursor-pointer">Active</Label>
            <Controller control={control} name="isActive" render={({ field }) => <Switch id="dp-isActive" checked={field.value} onCheckedChange={field.onChange} />} />
          </div>

          <DialogFooter>
            <Button type="button" variant="outline" onClick={onClose} disabled={mutation.isPending}>
              Cancel
            </Button>
            <Button type="submit" disabled={mutation.isPending}>
              {mutation.isPending ? 'Saving…' : mode === 'create' ? 'Create' : 'Save Changes'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
