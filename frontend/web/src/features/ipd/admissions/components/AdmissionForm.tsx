import { ApiError, createAdmissionSchema, IPD_ADMISSION_TYPES, type AdmissionFormValues, type Bed } from '@hms/shared';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect, useState } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { BedSelect } from '@/components/BedSelect';
import { ConsultantSelect } from '@/components/ConsultantSelect';
import { DepartmentSelect } from '@/components/DepartmentSelect';
import { WardSelect } from '@/components/WardSelect';

interface AdmissionFormProps {
  patientId: string;
  onSubmit: (values: AdmissionFormValues) => void;
  isSubmitting: boolean;
  apiError: ApiError | null;
}

export function AdmissionForm({ patientId, onSubmit, isSubmitting, apiError }: AdmissionFormProps) {
  const {
    control,
    register,
    handleSubmit,
    setError,
    setValue,
    watch,
    formState: { errors },
  } = useForm<AdmissionFormValues>({
    resolver: zodResolver(createAdmissionSchema),
    defaultValues: {
      patientId,
      departmentId: '',
      consultantId: '',
      wardId: '',
      bedId: '',
      admissionDateTime: '',
      admissionType: 'Elective',
      reasonForAdmission: '',
    },
  });

  const wardId = watch('wardId');
  const bedId = watch('bedId');
  const departmentId = watch('departmentId');
  const [availableBeds, setAvailableBeds] = useState<Bed[]>([]);
  const selectedBed = availableBeds.find((bed) => bed.id === bedId);

  // Server-side validation failures (docs/ApiStandards.md §5) are mapped onto the same
  // field-level display client validation uses, per docs/FrontendArchitecture.md §9.
  useEffect(() => {
    if (!apiError?.validationErrors) {
      return;
    }

    for (const issue of apiError.validationErrors) {
      const fieldName = (issue.field.charAt(0).toLowerCase() + issue.field.slice(1)) as keyof AdmissionFormValues;
      setError(fieldName, { type: 'server', message: issue.message });
    }
  }, [apiError, setError]);

  const generalError = apiError && !apiError.validationErrors ? apiError.message : null;

  function handleWardChange(newWardId: string, onChange: (value: string) => void) {
    onChange(newWardId);
    // A bed picked under the previous ward is meaningless once the ward changes.
    setValue('bedId', '');
  }

  function handleDepartmentChange(newDepartmentId: string, onChange: (value: string) => void) {
    onChange(newDepartmentId);
    // A consultant picked under the previous department is meaningless once the
    // department changes.
    setValue('consultantId', '');
  }

  return (
    <form onSubmit={handleSubmit(onSubmit)} noValidate className="flex max-w-2xl flex-col gap-4">
      {generalError && (
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          {generalError}
        </p>
      )}

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="departmentId">Department</Label>
          <Controller
            control={control}
            name="departmentId"
            render={({ field }) => (
              <DepartmentSelect
                id="departmentId"
                value={field.value}
                onValueChange={(value) => handleDepartmentChange(value, field.onChange)}
              />
            )}
          />
          {errors.departmentId && <p className="text-sm text-destructive">{errors.departmentId.message}</p>}
        </div>

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="consultantId">Consultant</Label>
          <Controller
            control={control}
            name="consultantId"
            render={({ field }) => (
              <ConsultantSelect id="consultantId" value={field.value} onValueChange={field.onChange} departmentId={departmentId} />
            )}
          />
          {errors.consultantId && <p className="text-sm text-destructive">{errors.consultantId.message}</p>}
        </div>
      </div>

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="admissionType">Admission type</Label>
          <Controller
            control={control}
            name="admissionType"
            render={({ field }) => (
              <Select value={field.value} onValueChange={field.onChange}>
                <SelectTrigger id="admissionType" aria-label="Admission type">
                  <SelectValue placeholder="Select admission type…" />
                </SelectTrigger>
                <SelectContent>
                  {IPD_ADMISSION_TYPES.map((type) => (
                    <SelectItem key={type} value={type}>
                      {type}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            )}
          />
          {errors.admissionType && <p className="text-sm text-destructive">{errors.admissionType.message}</p>}
        </div>

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="admissionDateTime">Admission date/time</Label>
          <Input id="admissionDateTime" type="datetime-local" {...register('admissionDateTime')} />
          <p className="text-xs text-muted-foreground">Leave blank to use the current date/time.</p>
        </div>
      </div>

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="wardId">Ward</Label>
          <Controller
            control={control}
            name="wardId"
            render={({ field }) => <WardSelect id="wardId" value={field.value} onValueChange={(value) => handleWardChange(value, field.onChange)} />}
          />
          {errors.wardId && <p className="text-sm text-destructive">{errors.wardId.message}</p>}
        </div>

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="bedId">Available bed</Label>
          <Controller
            control={control}
            name="bedId"
            render={({ field }) => (
              <BedSelect id="bedId" value={field.value} onValueChange={field.onChange} wardId={wardId} onBedsLoaded={setAvailableBeds} />
            )}
          />
          {errors.bedId && <p className="text-sm text-destructive">{errors.bedId.message}</p>}
        </div>
      </div>

      {selectedBed && (
        <div className="rounded-md border border-border bg-muted/30 px-3 py-2.5 text-sm">
          Selected Bed Charge: <span className="font-semibold text-foreground">₹{selectedBed.dailyCharge.toLocaleString('en-IN')}/day</span> ({selectedBed.bedNumber})
        </div>
      )}

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="reasonForAdmission">Reason for admission</Label>
        <textarea
          id="reasonForAdmission"
          rows={3}
          {...register('reasonForAdmission')}
          className="flex w-full rounded-md border border-input bg-background px-3 py-2 text-sm shadow-sm placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
          placeholder="Clinical reason for this admission…"
        />
        {errors.reasonForAdmission && <p className="text-sm text-destructive">{errors.reasonForAdmission.message}</p>}
      </div>

      <div className="mt-2 flex gap-3">
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting ? 'Saving…' : 'Save Admission'}
        </Button>
      </div>
    </form>
  );
}
