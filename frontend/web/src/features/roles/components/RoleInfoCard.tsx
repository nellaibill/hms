import type { Control, FieldErrors, UseFormRegister } from 'react-hook-form';
import { Controller } from 'react-hook-form';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import type { RoleFormValues } from '../types';

interface RoleInfoCardProps {
  register: UseFormRegister<RoleFormValues>;
  control: Control<RoleFormValues>;
  errors: FieldErrors<RoleFormValues>;
  readOnly?: boolean;
}

/** "Role Information" card — name, description, status. Mirrors the Field (label + control + error) stack used throughout Patient Registration. */
export function RoleInfoCard({ register, control, errors, readOnly }: RoleInfoCardProps) {
  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-base">Role Information</CardTitle>
      </CardHeader>
      <CardContent className="flex flex-col gap-4">
        <div className="flex flex-wrap gap-4">
          <div className="flex min-w-[220px] flex-1 flex-col gap-1">
            <label htmlFor="roleName" className="text-sm font-medium leading-none text-foreground">
              Name <span className="text-destructive">*</span>
            </label>
            <Input id="roleName" disabled={readOnly} {...register('name', { required: 'Role name is required.' })} />
            {errors.name && <p className="text-sm text-destructive">{errors.name.message}</p>}
          </div>

          <div className="flex w-full flex-col gap-1 sm:w-48">
            <label htmlFor="roleStatus" className="text-sm font-medium leading-none text-foreground">
              Status
            </label>
            <Controller
              name="status"
              control={control}
              render={({ field }) => (
                <Select value={field.value} onValueChange={field.onChange} disabled={readOnly}>
                  <SelectTrigger id="roleStatus" aria-label="Status">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="Active">Active</SelectItem>
                    <SelectItem value="Inactive">Inactive</SelectItem>
                  </SelectContent>
                </Select>
              )}
            />
          </div>
        </div>

        <div className="flex flex-col gap-1">
          <label htmlFor="roleDescription" className="text-sm font-medium leading-none text-foreground">
            Description
          </label>
          <textarea
            id="roleDescription"
            disabled={readOnly}
            rows={2}
            placeholder="What this role is for and who typically holds it…"
            className="flex w-full resize-none rounded-md border border-input bg-background px-3 py-2 text-sm text-foreground shadow-sm transition-colors placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 focus-visible:ring-offset-background disabled:cursor-not-allowed disabled:opacity-50"
            {...register('description')}
          />
        </div>
      </CardContent>
    </Card>
  );
}
