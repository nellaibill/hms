import type { FormEvent } from 'react';
import { useMemo, useRef, useState } from 'react';
import type { Consultant, Patient } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { Search } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { Input } from '@/components/ui/input';
import { Popover, PopoverAnchor, PopoverContent } from '@/components/ui/popover';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { useAuth } from '@/features/auth/AuthContext';
import { formatConsultantAvailability } from '@/features/masters/formatConsultantAvailability';
import { useDebouncedValue } from '@/hooks/useDebouncedValue';
import { usePatientsQuery } from '@/features/patients/hooks/usePatientsQuery';
import { consultantsApi, departmentsApi } from '@/services/apiClient';

type SearchScope = 'patient' | 'doctor';

const SEARCH_PLACEHOLDERS: Record<SearchScope, string> = {
  patient: 'Search by Name, UHID, or Phone…',
  doctor: 'Search doctor by name…',
};

// The Masters Consultant view page is route-gated on this permission (routes.tsx mastersRoutes),
// so only users who hold it get clickable doctor results; everyone else still sees the list.
const CONSULTANT_PAGE_PERMISSION = 'identity-administration.view';

const SUGGESTION_ROW_CLASS = 'flex w-full flex-col items-start gap-0.5 rounded-sm px-2 py-1.5 text-left text-sm';

/** Single unified control — a scope selector (Patient/Doctor) and a text input sharing one bordered box.
 * Typing shows a debounced type-ahead dropdown for the chosen scope:
 * - Patient: the same OR-across-name/UHID/phone match PatientListToolbar's own suggestions use;
 *   picking a result jumps straight to that patient's record, and pressing Enter without picking
 *   one runs the same text as a full search on the Patient Enquiry page.
 * - Doctor: active consultants whose name matches, from the same GET /masters/consultants list
 *   the Masters > Consultant page uses, with specialization, department and visiting hours. Users
 *   who can open the Consultant page can pick a result to go to that consultant's record. */
export function HeaderSearchBox() {
  const navigate = useNavigate();
  const { hasPermission } = useAuth();
  const inputRef = useRef<HTMLInputElement>(null);
  const [scope, setScope] = useState<SearchScope>('patient');
  const [query, setQuery] = useState('');
  const [isFocused, setIsFocused] = useState(false);

  const debouncedQuery = useDebouncedValue(query, 250);
  const trimmedQuery = debouncedQuery.trim();
  const showSuggestions = isFocused && trimmedQuery.length > 0;
  const showPatientSuggestions = showSuggestions && scope === 'patient';
  const showDoctorSuggestions = showSuggestions && scope === 'doctor';

  const { data: patientData, isFetching: isFetchingPatients } = usePatientsQuery(
    { page: 1, pageSize: 25, sort: 'lastName', search: trimmedQuery },
    { enabled: showPatientSuggestions },
  );
  const patients = showPatientSuggestions ? (patientData?.items ?? []) : [];

  const { data: doctorData, isFetching: isFetchingDoctors } = useQuery({
    queryKey: ['consultants', 'header-search', trimmedQuery],
    queryFn: () => consultantsApi.getConsultants({ page: 1, pageSize: 25, search: trimmedQuery, isActive: true }),
    enabled: showDoctorSuggestions,
  });
  const doctors = showDoctorSuggestions ? (doctorData?.items ?? []) : [];

  // Same cached ['departments','select-list'] query DepartmentName/DepartmentSelect use, so the
  // department names cost no extra request once any department picker has loaded.
  const { data: departmentData } = useQuery({
    queryKey: ['departments', 'select-list'],
    queryFn: () => departmentsApi.getDepartments({ pageSize: 100, isActive: true }),
    enabled: showDoctorSuggestions,
  });
  const departmentNames = useMemo(
    () => new Map((departmentData?.items ?? []).map((department) => [department.id, department.name])),
    [departmentData],
  );

  const canOpenConsultant = hasPermission(CONSULTANT_PAGE_PERMISSION);
  const isFetching = scope === 'patient' ? isFetchingPatients : isFetchingDoctors;

  function closeSuggestions() {
    setQuery('');
    setIsFocused(false);
  }

  function goToPatient(patient: Patient) {
    closeSuggestions();
    navigate(`/patients/registration/${patient.id}`);
  }

  function goToDoctor(doctor: Consultant) {
    closeSuggestions();
    navigate(`/admin/masters/consultant/${doctor.id}`);
  }

  function handleSubmit(event: FormEvent) {
    event.preventDefault();
    if (scope !== 'patient') return;
    const trimmed = query.trim();
    if (!trimmed) return;
    setIsFocused(false);
    navigate('/patients/enquiry', { state: { name: trimmed } });
  }

  return (
    <form
      onSubmit={handleSubmit}
      className="flex w-full items-stretch overflow-hidden rounded-md border border-transparent bg-white shadow-sm"
    >
      <Select value={scope} onValueChange={(next) => setScope(next as SearchScope)}>
        <SelectTrigger
          aria-label="Search scope"
          className="w-[8.5rem] shrink-0 rounded-none lg:w-[9.5rem] border-0 border-r border-slate-200 bg-transparent text-slate-700 shadow-none focus:ring-0 focus:ring-offset-0"
        >
          <SelectValue />
        </SelectTrigger>
        <SelectContent
          // Hand focus back to the text box (not the scope trigger) once a scope is picked, so
          // already-typed text immediately shows results for the new scope and typing continues.
          onCloseAutoFocus={(event) => {
            event.preventDefault();
            inputRef.current?.focus();
          }}
        >
          <SelectItem value="patient">Patient Search</SelectItem>
          <SelectItem value="doctor">Doctor Search</SelectItem>
        </SelectContent>
      </Select>
      <div className="relative min-w-[6rem] flex-1">
        <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
        <Popover open={showSuggestions}>
          <PopoverAnchor asChild>
            <Input
              ref={inputRef}
              type="search"
              placeholder={SEARCH_PLACEHOLDERS[scope]}
              aria-label={scope === 'patient' ? 'Patient search' : 'Doctor search'}
              value={query}
              onChange={(event) => setQuery(event.target.value)}
              onFocus={() => setIsFocused(true)}
              onBlur={() => setIsFocused(false)}
              autoComplete="off"
              className="h-full rounded-none border-0 bg-transparent pl-9 text-slate-900 shadow-none placeholder:text-slate-400 focus-visible:ring-0 focus-visible:ring-offset-0"
            />
          </PopoverAnchor>
          <PopoverContent
            align="start"
            className="w-[--radix-popover-trigger-width] p-1"
            // Both of these would otherwise steal keyboard focus away from the text input the
            // moment this popover opens/closes — the whole point here is that the input stays
            // focused and keeps receiving keystrokes the entire time this is open.
            onOpenAutoFocus={(event) => event.preventDefault()}
            onCloseAutoFocus={(event) => event.preventDefault()}
          >
            {isFetching ? (
              <p className="px-2 py-3 text-center text-sm text-muted-foreground">Searching…</p>
            ) : scope === 'patient' ? (
              <PatientSuggestions patients={patients} onPick={goToPatient} />
            ) : (
              <DoctorSuggestions
                doctors={doctors}
                departmentNames={departmentNames}
                onPick={canOpenConsultant ? goToDoctor : undefined}
              />
            )}
          </PopoverContent>
        </Popover>
      </div>
    </form>
  );
}

function EmptySuggestions({ label }: { label: string }) {
  return <p className="px-2 py-3 text-center text-sm text-muted-foreground">{label}</p>;
}

function PatientSuggestions({ patients, onPick }: { patients: Patient[]; onPick: (patient: Patient) => void }) {
  if (patients.length === 0) return <EmptySuggestions label="No matching patients" />;

  return (
    <ul role="listbox" className="max-h-64 overflow-y-auto">
      {patients.map((patient) => (
        <li key={patient.id}>
          <button
            type="button"
            role="option"
            aria-selected={false}
            // onMouseDown (not onClick) fires before the input's onBlur would — without
            // preventDefault here, the input blurs (closing this popover) before the
            // click below ever registers, so nothing would ever get selected.
            onMouseDown={(event) => event.preventDefault()}
            onClick={() => onPick(patient)}
            className={`${SUGGESTION_ROW_CLASS} hover:bg-sidebar-active hover:text-sidebar-active-foreground`}
          >
            <span className="font-medium text-foreground">
              {patient.title} {patient.firstName} {patient.lastName}
            </span>
            <span className="text-xs text-muted-foreground">
              {patient.uhid} · {patient.primaryPhone}
            </span>
          </button>
        </li>
      ))}
    </ul>
  );
}

interface DoctorSuggestionsProps {
  doctors: Consultant[];
  departmentNames: Map<string, string>;
  /** Omitted when the user can't open the Consultant page — the results are then a read-only list. */
  onPick?: (doctor: Consultant) => void;
}

function DoctorSuggestions({ doctors, departmentNames, onPick }: DoctorSuggestionsProps) {
  if (doctors.length === 0) return <EmptySuggestions label="No matching doctors" />;

  return (
    <ul role="listbox" aria-label="Doctors" className="max-h-72 overflow-y-auto">
      {doctors.map((doctor) => {
        const details = (
          <>
            <span className="font-medium text-foreground">{doctor.name}</span>
            <DoctorDetails doctor={doctor} departmentName={doctor.departmentId ? departmentNames.get(doctor.departmentId) : undefined} />
          </>
        );

        return (
          <li key={doctor.id}>
            {onPick ? (
              <button
                type="button"
                role="option"
                aria-selected={false}
                // Same blur-before-click guard as PatientSuggestions above.
                onMouseDown={(event) => event.preventDefault()}
                onClick={() => onPick(doctor)}
                className={`${SUGGESTION_ROW_CLASS} hover:bg-sidebar-active hover:text-sidebar-active-foreground`}
              >
                {details}
              </button>
            ) : (
              <div role="option" aria-selected={false} className={SUGGESTION_ROW_CLASS}>
                {details}
              </div>
            )}
          </li>
        );
      })}
    </ul>
  );
}

function DoctorDetails({ doctor, departmentName }: { doctor: Consultant; departmentName?: string }) {
  const specialty = [doctor.specialization, departmentName].filter(Boolean).join(' · ');
  const availability = formatConsultantAvailability(doctor);

  return (
    <>
      {specialty && <span className="text-xs text-muted-foreground">{specialty}</span>}
      {availability && <span className="text-xs text-muted-foreground">{availability}</span>}
    </>
  );
}
