import { ApiError, createDiagnosticPackageSchema, type DiagnosticPackageFormValues } from '@hms/shared';
import { zodResolver } from '@hookform/resolvers/zod';
import { Controller, useForm } from 'react-hook-form';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Switch } from '@/components/ui/switch';

interface DiagnosticPackageFormProps {
  defaultValues?: Partial<DiagnosticPackageFormValues>;
  mode: 'create' | 'edit';
  onSubmit: (values: DiagnosticPackageFormValues) => void;
  isSubmitting: boolean;
  submitLabel: string;
  apiError: ApiError | null;
}

/** Central Laboratory's Package create/edit form (Code/Name/Description/Total Price/Active
 * only — included tests are added afterward on LabPackageDetailPage, matching the mockup's own
 * flow where a package is created first, then tests are added on its detail page). */
export function DiagnosticPackageForm({ defaultValues, mode, onSubmit, isSubmitting, submitLabel, apiError }: DiagnosticPackageFormProps) {
  const codeReadOnly = mode === 'edit';

  const {
    register,
    control,
    handleSubmit,
    formState: { errors },
  } = useForm<DiagnosticPackageFormValues>({
    resolver: zodResolver(createDiagnosticPackageSchema),
    defaultValues: {
      code: '',
      name: '',
      description: '',
      totalPrice: 0,
      isActive: true,
      ...defaultValues,
    },
  });

  const generalError = apiError && !apiError.validationErrors ? apiError.message : null;

  return (
    <form onSubmit={handleSubmit(onSubmit)} noValidate className="mx-auto flex w-full max-w-3xl flex-col gap-5">
      {generalError && (
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          {generalError}
        </p>
      )}

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Package Details</CardTitle>
        </CardHeader>
        <CardContent className="flex flex-wrap gap-4">
          <div className="flex min-w-[200px] flex-1 flex-col gap-1">
            <Label htmlFor="dpk-code">Code</Label>
            <Input id="dpk-code" disabled={codeReadOnly} {...register('code')} />
            {errors.code && <p className="text-sm text-destructive">{errors.code.message}</p>}
          </div>

          <div className="flex min-w-[200px] flex-1 flex-col gap-1">
            <Label htmlFor="dpk-name">Name</Label>
            <Input id="dpk-name" {...register('name')} />
            {errors.name && <p className="text-sm text-destructive">{errors.name.message}</p>}
          </div>

          <div className="flex min-w-[280px] flex-[2] flex-col gap-1">
            <Label htmlFor="dpk-description">Description</Label>
            <Input id="dpk-description" {...register('description')} />
            {errors.description && <p className="text-sm text-destructive">{errors.description.message}</p>}
          </div>

          <div className="flex min-w-[200px] flex-1 flex-col gap-1">
            <Label htmlFor="dpk-totalPrice">Total Price (₹)</Label>
            <Input id="dpk-totalPrice" type="number" min={0} step="any" {...register('totalPrice')} />
            {errors.totalPrice && <p className="text-sm text-destructive">{errors.totalPrice.message}</p>}
          </div>

          <div className="flex min-w-[200px] flex-col gap-1">
            <span className="text-sm font-medium leading-none text-foreground">Status</span>
            <Controller
              name="isActive"
              control={control}
              render={({ field }) => (
                <div className="flex h-10 items-center gap-2">
                  <Switch checked={field.value} onCheckedChange={field.onChange} aria-label="Active" />
                  <span className="text-sm text-muted-foreground">{field.value ? 'Active' : 'Inactive'}</span>
                </div>
              )}
            />
          </div>
        </CardContent>
      </Card>

      {mode === 'create' && (
        <p className="text-sm text-muted-foreground">
          Tests are added to the package after it's created — you'll be taken to the package's page to add them next.
        </p>
      )}

      <div className="sticky bottom-0 z-10 -mx-4 flex justify-end gap-3 border-t border-border bg-background/95 px-4 py-3 backdrop-blur supports-[backdrop-filter]:bg-background/80">
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting ? 'Saving…' : submitLabel}
        </Button>
      </div>
    </form>
  );
}
