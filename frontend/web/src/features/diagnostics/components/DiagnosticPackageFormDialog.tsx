import { ApiError, updateDiagnosticPackageSchema, type DiagnosticPackage, type DiagnosticPackageFormValues } from '@hms/shared';
import { zodResolver } from '@hookform/resolvers/zod';
import { Controller, useForm } from 'react-hook-form';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Switch } from '@/components/ui/switch';
import { useUpdateDiagnosticPackageMutation } from '../hooks/useDiagnosticPackageMutations';

interface DiagnosticPackageFormDialogProps {
  pkg: DiagnosticPackage;
  onClose: () => void;
}

/** Edit-only (Create has its own dedicated page, DiagnosticPackageCreatePage — see its own
 * comment for why) — the "Edit" button on LabPackageDetailPage's Package Information card
 * opens this, matching the lightweight dialog pattern Categories/External Labs use rather than
 * a full page, since Package's own editable fields (items aren't editable here — see
 * UpdateDiagnosticPackageRequest) are just as few. */
export function DiagnosticPackageFormDialog({ pkg, onClose }: DiagnosticPackageFormDialogProps) {
  const mutation = useUpdateDiagnosticPackageMutation();

  const {
    register,
    control,
    handleSubmit,
    formState: { errors },
  } = useForm<DiagnosticPackageFormValues>({
    resolver: zodResolver(updateDiagnosticPackageSchema),
    defaultValues: {
      code: pkg.code,
      name: pkg.name,
      description: pkg.description ?? '',
      totalPrice: pkg.totalPrice,
      isActive: pkg.isActive,
    },
  });

  function onSubmit(values: DiagnosticPackageFormValues) {
    mutation.mutate(
      {
        id: pkg.id,
        request: {
          code: pkg.code,
          name: values.name,
          description: values.description || undefined,
          totalPrice: values.totalPrice,
          isActive: values.isActive,
        },
      },
      { onSuccess: onClose },
    );
  }

  const apiError = mutation.error instanceof ApiError ? mutation.error : null;

  return (
    <Dialog open onOpenChange={(open) => !open && onClose()}>
      <DialogContent aria-labelledby="diagnostic-package-form-title">
        <DialogHeader>
          <DialogTitle id="diagnostic-package-form-title">Edit Package</DialogTitle>
        </DialogHeader>

        <form onSubmit={handleSubmit(onSubmit)} noValidate className="flex flex-col gap-4">
          {apiError && !apiError.validationErrors && (
            <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
              {apiError.message}
            </p>
          )}

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="dpke-code">Code</Label>
            <Input id="dpke-code" disabled {...register('code')} />
            <p className="text-xs text-muted-foreground">Code can't be changed after creation.</p>
          </div>

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="dpke-name">Name</Label>
            <Input id="dpke-name" {...register('name')} />
            {errors.name && <p className="text-sm text-destructive">{errors.name.message}</p>}
          </div>

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="dpke-description">Description</Label>
            <Input id="dpke-description" {...register('description')} />
            {errors.description && <p className="text-sm text-destructive">{errors.description.message}</p>}
          </div>

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="dpke-totalPrice">Total Price (₹)</Label>
            <Input id="dpke-totalPrice" type="number" min={0} step="any" {...register('totalPrice')} />
            {errors.totalPrice && <p className="text-sm text-destructive">{errors.totalPrice.message}</p>}
          </div>

          <div className="flex items-center justify-between rounded-md border border-border px-3 py-2.5">
            <Label htmlFor="dpke-isActive" className="cursor-pointer">Active</Label>
            <Controller control={control} name="isActive" render={({ field }) => <Switch id="dpke-isActive" checked={field.value} onCheckedChange={field.onChange} />} />
          </div>

          <DialogFooter>
            <Button type="button" variant="outline" onClick={onClose} disabled={mutation.isPending}>
              Cancel
            </Button>
            <Button type="submit" disabled={mutation.isPending}>
              {mutation.isPending ? 'Saving…' : 'Save Changes'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
