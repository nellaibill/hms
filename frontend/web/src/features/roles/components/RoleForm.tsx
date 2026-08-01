import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { Button } from '@/components/ui/button';
import { PermissionMatrix } from './PermissionMatrix';
import { RoleInfoCard } from './RoleInfoCard';
import { countGrantedPermissions, type PermissionAction, type RoleFormValues } from '../types';

interface RoleFormProps {
  mode: 'create' | 'edit' | 'view';
  defaultValues: RoleFormValues;
  isSubmitting?: boolean;
  onSubmit: (values: RoleFormValues) => void;
  onCancel: () => void;
}

export function RoleForm({ mode, defaultValues, isSubmitting, onSubmit, onCancel }: RoleFormProps) {
  const readOnly = mode === 'view';
  const {
    register,
    control,
    handleSubmit,
    watch,
    setValue,
    formState: { errors },
  } = useForm<RoleFormValues>({ defaultValues });
  // Tracked separately from react-hook-form's own errors — `permissions` is a nested
  // module/action grid, not a leaf field, so RHF's FieldError typing doesn't fit it cleanly.
  const [permissionsError, setPermissionsError] = useState<string | null>(null);

  const permissions = watch('permissions');

  function handlePermissionChange(moduleId: string, action: PermissionAction, value: boolean) {
    setValue(
      'permissions',
      { ...permissions, [moduleId]: { ...permissions[moduleId], [action]: value } },
      { shouldDirty: true },
    );
    setPermissionsError(null);
  }

  // Backend rejects a role with zero granted permissions — catch it here with a clear
  // message instead of letting it fail opaquely after a round trip to the API.
  function handleValidatedSubmit(values: RoleFormValues) {
    if (countGrantedPermissions(values.permissions).granted === 0) {
      setPermissionsError('Select at least one permission before saving.');
      return;
    }
    onSubmit(values);
  }

  return (
    <form onSubmit={handleSubmit(handleValidatedSubmit)} noValidate className="mx-auto flex w-full max-w-5xl flex-col gap-5">
      <RoleInfoCard register={register} control={control} errors={errors} readOnly={readOnly} />
      <PermissionMatrix permissions={permissions} onChange={handlePermissionChange} readOnly={readOnly} />
      {permissionsError && (
        <p role="alert" className="-mt-2 text-sm text-destructive">
          {permissionsError}
        </p>
      )}

      {!readOnly && (
        <div className="sticky bottom-0 z-10 -mx-4 flex justify-end gap-3 border-t border-border bg-background/95 px-4 py-3 backdrop-blur supports-[backdrop-filter]:bg-background/80">
          <Button type="button" variant="outline" onClick={onCancel}>
            Cancel
          </Button>
          <Button type="submit" disabled={isSubmitting}>
            {isSubmitting ? 'Saving…' : 'Save'}
          </Button>
        </div>
      )}
    </form>
  );
}
