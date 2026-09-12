import { ApiError, IPD_ADMISSION_TYPES, type IpdAdmissionType, type Patient, type RequestAdmissionRequest } from '@hms/shared';
import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { ConsultantSelect } from '@/components/ConsultantSelect';
import { DepartmentSelect } from '@/components/DepartmentSelect';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { PatientPicker } from '@/features/billing';

interface RequestAdmissionDialogProps {
  isSubmitting: boolean;
  apiError: ApiError | null;
  onSubmit: (request: RequestAdmissionRequest) => void;
  onCancel: () => void;
}

/** OPD Admissions List tab's "Request Admission" action — raises a Requested admission with
 * no ward/bed yet (staff assign a bed later from IPD's own Admissions List "Requested" tab).
 * Two steps in one dialog, mirroring AdmissionCreatePage's own patient-first flow: pick the
 * patient via the same PatientPicker Billing uses, then fill in department/consultant/type/
 * reason. */
export function RequestAdmissionDialog({ isSubmitting, apiError, onSubmit, onCancel }: RequestAdmissionDialogProps) {
  const [patient, setPatient] = useState<Patient | null>(null);
  const [departmentId, setDepartmentId] = useState('');
  const [consultantId, setConsultantId] = useState('');
  const [admissionType, setAdmissionType] = useState<IpdAdmissionType>('Elective');
  const [reasonForAdmission, setReasonForAdmission] = useState('');

  const generalError = apiError && !apiError.validationErrors ? apiError.message : null;
  const canSubmit = Boolean(patient && departmentId && consultantId && reasonForAdmission.trim());

  function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    if (!patient || !canSubmit) {
      return;
    }
    onSubmit({
      patientId: patient.id,
      departmentId,
      consultantId,
      admissionType,
      reasonForAdmission: reasonForAdmission.trim(),
    });
  }

  return (
    <Dialog open onOpenChange={(open) => !open && onCancel()}>
      <DialogContent aria-labelledby="request-admission-title" className="max-w-2xl">
        <DialogHeader>
          <DialogTitle id="request-admission-title">Request Admission</DialogTitle>
          <DialogDescription>Raise an inpatient admission request — a ward/bed is assigned separately once approved.</DialogDescription>
        </DialogHeader>

        {generalError && (
          <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
            {generalError}
          </p>
        )}

        {!patient && (
          <div className="max-h-[60vh] overflow-y-auto">
            <PatientPicker onSelect={setPatient} />
          </div>
        )}

        {patient && (
          <form onSubmit={handleSubmit} className="flex flex-col gap-4">
            <div className="flex items-center justify-between gap-3 rounded-md border border-border bg-muted/30 px-3 py-2.5 text-sm">
              <span className="font-medium text-foreground">
                {patient.title} {patient.firstName} {patient.lastName} · {patient.uhid}
              </span>
              <Button type="button" variant="outline" size="sm" onClick={() => setPatient(null)}>
                Change patient
              </Button>
            </div>

            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="request-department">Department</Label>
                <DepartmentSelect
                  id="request-department"
                  value={departmentId}
                  onValueChange={(value) => {
                    setDepartmentId(value);
                    setConsultantId('');
                  }}
                />
              </div>

              <div className="flex flex-col gap-1.5">
                <Label htmlFor="request-consultant">Consultant</Label>
                <ConsultantSelect id="request-consultant" value={consultantId} onValueChange={setConsultantId} departmentId={departmentId} />
              </div>
            </div>

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="request-admission-type">Admission type</Label>
              <Select value={admissionType} onValueChange={(value) => setAdmissionType(value as IpdAdmissionType)}>
                <SelectTrigger id="request-admission-type" aria-label="Admission type">
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
            </div>

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="request-reason">Reason for admission</Label>
              <textarea
                id="request-reason"
                rows={3}
                value={reasonForAdmission}
                onChange={(event) => setReasonForAdmission(event.target.value)}
                className="flex w-full rounded-md border border-input bg-background px-3 py-2 text-sm shadow-sm placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
                placeholder="Clinical reason for this admission…"
              />
            </div>

            <DialogFooter>
              <Button type="button" variant="outline" onClick={onCancel} disabled={isSubmitting}>
                Cancel
              </Button>
              <Button type="submit" disabled={isSubmitting || !canSubmit}>
                {isSubmitting ? 'Submitting…' : 'Request Admission'}
              </Button>
            </DialogFooter>
          </form>
        )}
      </DialogContent>
    </Dialog>
  );
}
