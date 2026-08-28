import { ApiError, createDiagnosticCategorySchema, updateDiagnosticCategorySchema, type DiagnosticCategory, type DiagnosticCategoryFormValues } from '@hms/shared';
import { zodResolver } from '@hookform/resolvers/zod';
import { Controller, useForm } from 'react-hook-form';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Switch } from '@/components/ui/switch';
import { useCreateDiagnosticCategoryMutation, useUpdateDiagnosticCategoryMutation } from '../hooks/useDiagnosticCategoryMutations';

interface DiagnosticCategoryFormDialogProps {
  mode: 'create' | 'edit';
  /** Required when mode is 'edit'. */
  category?: DiagnosticCategory;
  onClose: () => void;
}

/** DiagnosticCategory is tiny (3 real fields) — a single list page with this create/edit modal
 * is enough, rather than dedicated create/edit/view route pages. Mirrors LeaveTypeFormDialog. */
export function DiagnosticCategoryFormDialog({ mode, category, onClose }: DiagnosticCategoryFormDialogProps) {
  const createMutation = useCreateDiagnosticCategoryMutation();
  const updateMutation = useUpdateDiagnosticCategoryMutation();
  const mutation = mode === 'create' ? createMutation : updateMutation;

  const {
    register,
    control,
    handleSubmit,
    formState: { errors },
  } = useForm<DiagnosticCategoryFormValues>({
    resolver: zodResolver(mode === 'create' ? createDiagnosticCategorySchema : updateDiagnosticCategorySchema),
    defaultValues: {
      code: category?.code ?? '',
      name: category?.name ?? '',
      description: category?.description ?? '',
      isActive: category?.isActive ?? true,
    },
  });

  function onSubmit(values: DiagnosticCategoryFormValues) {
    const description = values.description || undefined;

    if (mode === 'create') {
      createMutation.mutate({ code: values.code, name: values.name, description, isActive: values.isActive }, { onSuccess: onClose });
    } else if (category) {
      updateMutation.mutate(
        { id: category.id, request: { code: category.code, name: values.name, description, isActive: values.isActive } },
        { onSuccess: onClose },
      );
    }
  }

  const apiError = mutation.error instanceof ApiError ? mutation.error : null;

  return (
    <Dialog open onOpenChange={(open) => !open && onClose()}>
      <DialogContent aria-labelledby="diagnostic-category-form-title">
        <DialogHeader>
          <DialogTitle id="diagnostic-category-form-title">{mode === 'create' ? 'New Category' : 'Edit Category'}</DialogTitle>
        </DialogHeader>

        <form onSubmit={handleSubmit(onSubmit)} noValidate className="flex flex-col gap-4">
          {apiError && !apiError.validationErrors && (
            <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
              {apiError.message}
            </p>
          )}

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="dc-code">Code</Label>
            <Input id="dc-code" disabled={mode === 'edit'} {...register('code')} />
            {mode === 'edit' && <p className="text-xs text-muted-foreground">Code can't be changed after creation.</p>}
            {errors.code && <p className="text-sm text-destructive">{errors.code.message}</p>}
          </div>

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="dc-name">Name</Label>
            <Input id="dc-name" {...register('name')} />
            {errors.name && <p className="text-sm text-destructive">{errors.name.message}</p>}
          </div>

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="dc-description">Description</Label>
            <Input id="dc-description" {...register('description')} />
            {errors.description && <p className="text-sm text-destructive">{errors.description.message}</p>}
          </div>

          <div className="flex items-center justify-between rounded-md border border-border px-3 py-2.5">
            <Label htmlFor="dc-isActive" className="cursor-pointer">Active</Label>
            <Controller control={control} name="isActive" render={({ field }) => <Switch id="dc-isActive" checked={field.value} onCheckedChange={field.onChange} />} />
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
