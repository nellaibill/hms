import type { Patient } from '@hms/shared';
import { forwardRef } from 'react';
import { patientsApi } from '@/services/apiClient';
import { LookupInput, type LookupInputHandle } from '../common/LookupInput';
import type { CounterPatient } from './cart';
import { toCounterPatient } from './patients';

async function searchPatients(term: string): Promise<Patient[]> {
  const page = await patientsApi.getPatients({ search: term, pageSize: 12 });
  return page.items;
}

interface PatientLookupProps {
  onSelect: (patient: CounterPatient) => void;
}

/** Finds a registered patient by name, UHID or phone for the bill. */
export const PatientLookup = forwardRef<LookupInputHandle, PatientLookupProps>(function PatientLookup({ onSelect }, ref) {
  return (
    <LookupInput<Patient>
      ref={ref}
      queryKey={['pharmacy', 'counter', 'patients']}
      search={searchPatients}
      getKey={(patient) => patient.id}
      onSelect={(patient) => onSelect(toCounterPatient(patient))}
      ariaLabel="Find patient"
      placeholder="Patient name, UHID or phone…"
      minChars={2}
      emptyText="No patient found. Bill as a walk-in, or register the patient first."
      inputClassName="h-9"
      renderItem={(patient) => (
        <div className="flex items-baseline justify-between gap-3">
          <span className="font-medium">
            {patient.firstName} {patient.lastName}
          </span>
          <span className="shrink-0 text-xs text-muted-foreground">
            {patient.uhid} · {patient.age}y {patient.gender} · {patient.primaryPhone}
          </span>
        </div>
      )}
    />
  );
});
