import type { Patient } from '@hms/shared';
import { Loader2, Search } from 'lucide-react';
import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { PatientListToolbar, emptyPatientSearchFilters, usePatientsQuery, type PatientSearchFilters } from '@/features/patients';

interface PatientPickerProps {
  onSelect: (patient: Patient) => void;
}

const RESULTS_PAGE_SIZE = 10;

/**
 * Reuses the same search-by-Name/Age/UHID/Phone toolbar as Old Patient Registration
 * (features/patients) — billing needs to find an existing patient, not manage them, so
 * results render as a pick list here rather than PatientTable's full row-actions view.
 *
 * Query construction deliberately differs from that page, though: when exactly one of the
 * free-text fields (Name/UHID/Phone) is filled in — and Age is not, since it's a numeric
 * date-of-birth-range filter the general search can't express — it's sent as the backend's
 * general `search` param (PatientRepository.GetPagedAsync OR-matches name/UHID/phone for
 * that one term) instead of the field-specific one. Found live: a real user pasted a UHID
 * into the "Patient Name" box (the natural thing to do when you just want to find someone
 * fast) and got a silent "no results", because the Name filter only matches First/LastName,
 * never UHID. Once more than one field has a value (or Age is one of them), this falls back
 * to the precise per-field AND-narrowing PatientsListPage also uses — a receptionist
 * deliberately combining Name + Phone to narrow among several matches should still get that
 * behavior, not a broad OR across unrelated fields.
 */
export function PatientPicker({ onSelect }: PatientPickerProps) {
  const [filters, setFilters] = useState<PatientSearchFilters>(emptyPatientSearchFilters);
  const [activeFilters, setActiveFilters] = useState<PatientSearchFilters | null>(null);
  const hasSearched = activeFilters !== null;

  const freeTextFields = activeFilters ? ([activeFilters.name, activeFilters.uhid, activeFilters.phone] as const) : undefined;
  const filledFreeTextCount = freeTextFields?.filter((value) => value.trim() !== '').length ?? 0;
  const ageIsFilled = Boolean(activeFilters?.age.trim());
  const singleFreeTextValue =
    !ageIsFilled && filledFreeTextCount === 1 ? freeTextFields?.find((value) => value.trim() !== '') : undefined;

  const { data, isPending, isError } = usePatientsQuery(
    {
      page: 1,
      pageSize: RESULTS_PAGE_SIZE,
      sort: 'lastName',
      requiresDataVerification: activeFilters?.needsVerification || undefined,
      ...(singleFreeTextValue
        ? { search: singleFreeTextValue.trim() }
        : {
            name: activeFilters?.name.trim() || undefined,
            age: activeFilters?.age.trim() ? Number(activeFilters.age) : undefined,
            uhid: activeFilters?.uhid.trim() || undefined,
            phone: activeFilters?.phone.trim() || undefined,
          }),
    },
    { enabled: hasSearched },
  );

  function handleFilterChange(field: keyof PatientSearchFilters, value: string | boolean) {
    setFilters((prev) => ({ ...prev, [field]: value }));
  }

  function handleSearch() {
    setActiveFilters(filters);
  }

  function handleClear() {
    setFilters(emptyPatientSearchFilters);
    setActiveFilters(null);
  }

  return (
    <div className="flex flex-col gap-4">
      <PatientListToolbar
        filters={filters}
        onFilterChange={handleFilterChange}
        onSearch={handleSearch}
        onClear={handleClear}
        onSuggestionSelect={onSelect}
      />

      {!hasSearched && (
        <Card className="border-dashed">
          <CardContent className="flex flex-col items-center gap-2 py-16 text-center">
            <Search className="h-6 w-6 text-muted-foreground" />
            <p className="text-sm font-medium text-foreground">Search for the patient to bill</p>
            <p className="text-sm text-muted-foreground">Enter a name, age, UHID, or phone number above, then click Search.</p>
          </CardContent>
        </Card>
      )}

      {hasSearched && isPending && (
        <div className="flex items-center justify-center gap-2 py-16 text-sm text-muted-foreground">
          <Loader2 className="h-4 w-4 animate-spin" />
          Searching…
        </div>
      )}

      {hasSearched && isError && (
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          Failed to search patients.
        </p>
      )}

      {hasSearched && !isPending && !isError && data && data.items.length === 0 && (
        <Card className="border-dashed">
          <CardContent className="flex flex-col items-center gap-2 py-16 text-center">
            <p className="text-sm font-medium text-foreground">No patients found matching the search criteria.</p>
            <p className="text-sm text-muted-foreground">Try a different or broader combination of search fields.</p>
          </CardContent>
        </Card>
      )}

      {hasSearched && !isPending && !isError && data && data.items.length > 0 && (
        <div className="flex flex-col divide-y divide-border overflow-hidden rounded-lg border border-border">
          {data.items.map((patient) => (
            <div key={patient.id} className="flex items-center justify-between gap-3 px-4 py-3 hover:bg-muted/30">
              <div className="flex flex-col gap-0.5">
                <span className="font-medium text-foreground">
                  {patient.title} {patient.firstName} {patient.lastName}
                </span>
                <span className="text-xs text-muted-foreground">
                  {patient.uhid} · {patient.primaryPhone}
                </span>
              </div>
              <Button size="sm" onClick={() => onSelect(patient)}>
                Select
              </Button>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
