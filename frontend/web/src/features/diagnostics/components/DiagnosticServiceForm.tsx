import { ApiError, createDiagnosticServiceSchema, DIAGNOSTIC_SERVICE_TYPES, type DiagnosticServiceFormValues } from '@hms/shared';
import { zodResolver } from '@hookform/resolvers/zod';
import { Controller, useForm } from 'react-hook-form';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Switch } from '@/components/ui/switch';
import { useDiagnosticCategoriesQuery } from '../hooks/useDiagnosticCategoriesQuery';
import { useDiagnosticProvidersQuery } from '../hooks/useDiagnosticProvidersQuery';

interface DiagnosticServiceFormProps {
  defaultValues?: Partial<DiagnosticServiceFormValues>;
  mode: 'create' | 'edit';
  onSubmit: (values: DiagnosticServiceFormValues) => void;
  isSubmitting: boolean;
  submitLabel: string;
  apiError: ApiError | null;
}

/** Central Laboratory's Service create/edit form (Code/Name/Category/Service Type/Outsourced/
 * Provider/Price/Active) — dedicated pages rather than a dialog, matching DiagnosticPackage's
 * own reasoning (more fields than the tiny Category/Provider masters). Mirrors ProductForm's
 * shape, but reads Category/Provider from the new typed diagnostics queries instead of the
 * generic Masters engine. */
export function DiagnosticServiceForm({ defaultValues, mode, onSubmit, isSubmitting, submitLabel, apiError }: DiagnosticServiceFormProps) {
  const codeReadOnly = mode === 'edit';
  const categoriesQuery = useDiagnosticCategoriesQuery({ pageSize: 200, isActive: true, sort: 'name' });
  const providersQuery = useDiagnosticProvidersQuery({ pageSize: 200, isActive: true, sort: 'name' });

  const {
    register,
    control,
    handleSubmit,
    watch,
    formState: { errors },
  } = useForm<DiagnosticServiceFormValues>({
    resolver: zodResolver(createDiagnosticServiceSchema),
    defaultValues: {
      code: '',
      name: '',
      categoryId: '',
      serviceType: 'Laboratory',
      isOutsourced: false,
      providerId: '',
      price: 0,
      isActive: true,
      ...defaultValues,
    },
  });

  const isOutsourced = watch('isOutsourced');
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
          <CardTitle className="text-base">Service Details</CardTitle>
        </CardHeader>
        <CardContent className="flex flex-wrap gap-4">
          <div className="flex min-w-[200px] flex-1 flex-col gap-1">
            <Label htmlFor="ds-code">Code</Label>
            <Input id="ds-code" disabled={codeReadOnly} {...register('code')} />
            {errors.code && <p className="text-sm text-destructive">{errors.code.message}</p>}
          </div>

          <div className="flex min-w-[200px] flex-1 flex-col gap-1">
            <Label htmlFor="ds-name">Name</Label>
            <Input id="ds-name" {...register('name')} />
            {errors.name && <p className="text-sm text-destructive">{errors.name.message}</p>}
          </div>

          <div className="flex min-w-[200px] flex-1 flex-col gap-1">
            <Label htmlFor="ds-category">Category</Label>
            <Controller
              name="categoryId"
              control={control}
              render={({ field }) => (
                <Select value={field.value} onValueChange={field.onChange} disabled={categoriesQuery.isPending}>
                  <SelectTrigger id="ds-category" aria-label="Category">
                    <SelectValue placeholder="Select category…" />
                  </SelectTrigger>
                  <SelectContent>
                    {(categoriesQuery.data?.items ?? []).map((category) => (
                      <SelectItem key={category.id} value={category.id}>
                        {category.name}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              )}
            />
            {errors.categoryId && <p className="text-sm text-destructive">{errors.categoryId.message}</p>}
          </div>

          <div className="flex min-w-[200px] flex-1 flex-col gap-1">
            <Label htmlFor="ds-serviceType">Service Type</Label>
            <Controller
              name="serviceType"
              control={control}
              render={({ field }) => (
                <Select value={field.value} onValueChange={field.onChange}>
                  <SelectTrigger id="ds-serviceType" aria-label="Service Type">
                    <SelectValue placeholder="Select type…" />
                  </SelectTrigger>
                  <SelectContent>
                    {DIAGNOSTIC_SERVICE_TYPES.map((type) => (
                      <SelectItem key={type} value={type}>
                        {type}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              )}
            />
            {errors.serviceType && <p className="text-sm text-destructive">{errors.serviceType.message}</p>}
          </div>

          <div className="flex min-w-[200px] flex-1 flex-col gap-1">
            <Label htmlFor="ds-price">Price (₹)</Label>
            <Input id="ds-price" type="number" min={0} step="any" {...register('price')} />
            {errors.price && <p className="text-sm text-destructive">{errors.price.message}</p>}
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Outsourcing</CardTitle>
        </CardHeader>
        <CardContent className="flex flex-wrap gap-4">
          <div className="flex min-w-[200px] flex-col gap-1">
            <span className="text-sm font-medium leading-none text-foreground">Outsourced</span>
            <Controller
              name="isOutsourced"
              control={control}
              render={({ field }) => (
                <div className="flex h-10 items-center gap-2">
                  <Switch checked={field.value} onCheckedChange={field.onChange} aria-label="Outsourced" />
                  <span className="text-sm text-muted-foreground">{field.value ? 'Yes' : 'No'}</span>
                </div>
              )}
            />
          </div>

          {isOutsourced && (
            <div className="flex min-w-[200px] flex-1 flex-col gap-1">
              <Label htmlFor="ds-provider">Provider (External Lab)</Label>
              <Controller
                name="providerId"
                control={control}
                render={({ field }) => (
                  <Select value={field.value || undefined} onValueChange={field.onChange} disabled={providersQuery.isPending}>
                    <SelectTrigger id="ds-provider" aria-label="Provider">
                      <SelectValue placeholder="Select provider…" />
                    </SelectTrigger>
                    <SelectContent>
                      {(providersQuery.data?.items ?? []).map((provider) => (
                        <SelectItem key={provider.id} value={provider.id}>
                          {provider.name}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                )}
              />
              {errors.providerId && <p className="text-sm text-destructive">{errors.providerId.message}</p>}
            </div>
          )}

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

      <div className="sticky bottom-0 z-10 -mx-4 flex justify-end gap-3 border-t border-border bg-background/95 px-4 py-3 backdrop-blur supports-[backdrop-filter]:bg-background/80">
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting ? 'Saving…' : submitLabel}
        </Button>
      </div>
    </form>
  );
}
