import type { Patient } from '@hms/shared';
import { ScanLine, X } from 'lucide-react';
import { useState } from 'react';
import { PageBanner } from '@/components/PageBanner';
import { Button } from '@/components/ui/button';
import { PatientPicker } from '@/features/billing';
import { PatientXrayImages } from '@/features/radiology';

/** Radiology ('/diagnostics/radiology'): pick a patient, see their stored images, and get an
 * AI-drafted read of each — saved to the patient's record — for the clinician to review. */
export default function RadiologyPage() {
  const [patient, setPatient] = useState<Patient | null>(null);

  return (
    <div className="flex flex-1 flex-col">
      <PageBanner icon={ScanLine} title="Radiology" subtitle="Patient X-ray images with AI-assisted reading for clinician review." />

      <div className="flex flex-1 flex-col gap-5 p-6 lg:p-8">
        {patient ? (
          <>
            <div className="flex flex-wrap items-center justify-between gap-3 rounded-lg border border-border bg-card px-4 py-3">
              <div className="flex items-center gap-3">
                <div className="flex h-10 w-10 items-center justify-center rounded-full bg-fuchsia-100 text-sm font-semibold text-fuchsia-700 dark:bg-fuchsia-500/20 dark:text-fuchsia-300" aria-hidden>
                  {`${patient.firstName[0] ?? ''}${patient.lastName[0] ?? ''}`.toUpperCase()}
                </div>
                <div>
                  <p className="font-medium text-foreground">
                    {patient.title} {patient.firstName} {patient.lastName}
                  </p>
                  <p className="text-xs text-muted-foreground">
                    UHID {patient.uhid} · {patient.age} Yrs · {patient.gender}
                  </p>
                </div>
              </div>
              <Button variant="outline" size="sm" onClick={() => setPatient(null)}>
                <X className="mr-2 h-4 w-4" />
                Change patient
              </Button>
            </div>
            <PatientXrayImages patientId={patient.id} />
          </>
        ) : (
          <PatientPicker onSelect={setPatient} />
        )}
      </div>
    </div>
  );
}
