import type { Patient } from '@hms/shared';
import { Loader2, Search } from 'lucide-react';
import { useState } from 'react';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { DistrictName } from '@/components/DistrictName';
import { StateName } from '@/components/StateName';
import { PatientListToolbar, emptyPatientSearchFilters, usePatientsQuery, type PatientSearchFilters } from '@/features/patients';
import { bloodGroupLabel } from '@/features/patients/bloodGroupLabel';
import { cn } from '@/lib/utils';
import { Pagination } from './Pagination';

interface PatientPickerProps {
  onSelect: (patient: Patient) => void;
}

const RESULTS_PAGE_SIZE = 20;
const RECENT_VISITS_PAGE_SIZE = 20;
const RECENT_VISITS_LIMIT = 100;
const RECENT_VISITS_MAX_PAGE = RECENT_VISITS_LIMIT / RECENT_VISITS_PAGE_SIZE;

/** A real `<table>` (header + aligned columns) rather than a flex row list — the previous
 * flex layout let each field's own content width push everything else out of vertical
 * alignment (a longer name shifted its row's Locality/Select out of line with the row above).
 * Shared by both the "Last 100 visits" default list and the search results below. */
function PatientPickerTable({ items, onSelect }: { items: Patient[]; onSelect: (patient: Patient) => void }) {
  return (
    <div className="overflow-hidden rounded-lg border border-border">
      <div className="overflow-x-auto">
        <table className="w-full text-sm">
          <thead className="bg-muted/60 text-left text-xs font-medium uppercase tracking-wide text-muted-foreground">
            <tr>
              <th className="px-4 py-2">Patient</th>
              <th className="px-4 py-2">Age / Gender</th>
              <th className="px-4 py-2">Phone</th>
              <th className="px-4 py-2">Blood Group</th>
              <th className="px-4 py-2">Locality</th>
              <th className="px-4 py-2 text-right">Action</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-border">
            {items.map((patient, index) => (
              <tr key={patient.id} className={cn('hover:bg-accent/60', index % 2 === 1 && 'bg-muted/40')}>
                <td className="px-4 py-2.5">
                  <div className="flex flex-col">
                    <span className="font-medium text-foreground">
                      {patient.title} {patient.firstName} {patient.lastName}
                    </span>
                    <span className="text-xs text-muted-foreground">{patient.uhid}</span>
                  </div>
                </td>
                <td className="whitespace-nowrap px-4 py-2.5 text-muted-foreground">
                  {patient.age} Yrs · {patient.gender}
                </td>
                <td className="whitespace-nowrap px-4 py-2.5 text-muted-foreground">{patient.primaryPhone}</td>
                <td className="px-4 py-2.5">
                  {patient.bloodGroup === 'Unknown' ? (
                    <span className="text-muted-foreground">—</span>
                  ) : (
                    <Badge variant="outline" className="text-[10px]">
                      {bloodGroupLabel(patient.bloodGroup)}
                    </Badge>
                  )}
                </td>
                <td className="whitespace-nowrap px-4 py-2.5 text-muted-foreground">
                  <DistrictName stateId={patient.address.stateId} districtId={patient.address.districtId} />, <StateName stateId={patient.address.stateId} />
                </td>
                <td className="px-4 py-2.5 text-right">
                  <Button size="sm" onClick={() => onSelect(patient)}>
                    Select
                  </Button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

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
  const [resultsPage, setResultsPage] = useState(1);
  const [recentPage, setRecentPage] = useState(1);
  const hasSearched = activeFilters !== null;

  const freeTextFields = activeFilters ? ([activeFilters.name, activeFilters.uhid, activeFilters.phone] as const) : undefined;
  const filledFreeTextCount = freeTextFields?.filter((value) => value.trim() !== '').length ?? 0;
  const ageIsFilled = Boolean(activeFilters?.age.trim());
  const singleFreeTextValue =
    !ageIsFilled && filledFreeTextCount === 1 ? freeTextFields?.find((value) => value.trim() !== '') : undefined;

  const { data, isPending, isError } = usePatientsQuery(
    {
      page: resultsPage,
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

  // Before any search is entered, default to the 100 most recently active patients — the
  // receptionist's most common billing task — instead of a blank "search first" prompt or a
  // today-only list that goes empty overnight. Driven by patients.patient_visits (not the
  // patient record's own timestamps), so a returning patient billed today shows up here too,
  // not just new registrations. Paged 20 at a time server-side (not fetched as one 100-row
  // page) and capped at RECENT_VISITS_MAX_PAGE below so the picker never pages further than
  // "the last 100" even though the server's own count reflects the whole patient list.
  const {
    data: recentData,
    isPending: isRecentPending,
    isError: isRecentError,
  } = usePatientsQuery({ page: recentPage, pageSize: RECENT_VISITS_PAGE_SIZE, sort: '-lastVisitAt' }, { enabled: !hasSearched });
  const recentMeta = recentData
    ? {
        ...recentData.meta,
        totalCount: Math.min(recentData.meta.totalCount, RECENT_VISITS_LIMIT),
        totalPages: Math.min(recentData.meta.totalPages, RECENT_VISITS_MAX_PAGE),
      }
    : undefined;

  function handleFilterChange(field: keyof PatientSearchFilters, value: string | boolean) {
    setFilters((prev) => ({ ...prev, [field]: value }));
  }

  function handleSearch() {
    setActiveFilters(filters);
    setResultsPage(1);
  }

  function handleClear() {
    setFilters(emptyPatientSearchFilters);
    setActiveFilters(null);
    setResultsPage(1);
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
        <div className="flex flex-col gap-2">
          <p className="text-sm font-medium text-foreground">Last 100 visits</p>

          {isRecentPending && (
            <div className="flex items-center justify-center gap-2 py-16 text-sm text-muted-foreground">
              <Loader2 className="h-4 w-4 animate-spin" />
              Loading…
            </div>
          )}

          {!isRecentPending && isRecentError && (
            <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
              Failed to load recent patients.
            </p>
          )}

          {!isRecentPending && !isRecentError && recentData && recentData.items.length === 0 && (
            <Card className="border-dashed">
              <CardContent className="flex flex-col items-center gap-2 py-16 text-center">
                <Search className="h-6 w-6 text-muted-foreground" />
                <p className="text-sm font-medium text-foreground">No patient visits recorded yet.</p>
                <p className="text-sm text-muted-foreground">Enter a name, age, UHID, or phone number above, then click Search.</p>
              </CardContent>
            </Card>
          )}

          {!isRecentPending && !isRecentError && recentData && recentData.items.length > 0 && (
            <>
              <PatientPickerTable items={recentData.items} onSelect={onSelect} />
              {recentMeta && <Pagination meta={recentMeta} onPageChange={setRecentPage} />}
            </>
          )}
        </div>
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
        <>
          <PatientPickerTable items={data.items} onSelect={onSelect} />
          <Pagination meta={data.meta} onPageChange={setResultsPage} />
        </>
      )}
    </div>
  );
}
