import type { Patient } from '@hms/shared';
import { ScanLine, X } from 'lucide-react';
import { useState } from 'react';
import { PageBanner } from '@/components/PageBanner';
import { Button } from '@/components/ui/button';
import { PatientPicker } from '@/features/billing';
import { PatientXrayImages } from '@/features/radiology';

/** Radiology ('/diagnostics/radiology'): pick a patient, see their stored images, and get an
 * AI-drafted read of each for the clinician to review. */
export default function RadiologyPage() {
  const [patient, setPatient] = useState<Patient | null>(null);

  return (
    <div className="flex flex-1 flex-col">
      <PageBanner icon={ScanLine} title="Radiology" subtitle="Patient X-ray images with AI-assisted reading for clinician review." />

      <div className="flex flex-1 flex-col gap-4 p-6 lg:p-8">
        {patient ? (
          <>
            <div className="flex flex-wrap items-center justify-between gap-2 rounded-lg border border-border bg-card px-4 py-3">
              <div>
                <p className="font-medium text-foreground">
                  {patient.title} {patient.firstName} {patient.lastName}
                </p>
                <p className="text-xs text-muted-foreground">
                  UHID {patient.uhid} · {patient.age} Yrs · {patient.gender}
                </p>
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
