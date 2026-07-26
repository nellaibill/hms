import { useForm } from 'react-hook-form';
import { Button } from '@/components/ui/button';
import { PermissionMatrix } from './PermissionMatrix';
import { RoleInfoCard } from './RoleInfoCard';
import type { PermissionAction, RoleFormValues } from '../types';

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

  const permissions = watch('permissions');

  function handlePermissionChange(moduleId: string, action: PermissionAction, value: boolean) {
    setValue(
      'permissions',
      { ...permissions, [moduleId]: { ...permissions[moduleId], [action]: value } },
      { shouldDirty: true },
    );
  }

  return (
    <form onSubmit={handleSubmit(onSubmit)} noValidate className="mx-auto flex w-full max-w-4xl flex-col gap-4">
      <RoleInfoCard register={register} control={control} errors={errors} readOnly={readOnly} />
      <PermissionMatrix permissions={permissions} onChange={handlePermissionChange} readOnly={readOnly} />

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
