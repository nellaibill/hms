import type { Patient } from '@hms/shared';
import { Plus, Search, X } from 'lucide-react';
import { useState } from 'react';
import { Link } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Popover, PopoverAnchor, PopoverContent } from '@/components/ui/popover';
import { useDebouncedValue } from '@/hooks/useDebouncedValue';
import { useAuth } from '../../auth/AuthContext';
import { usePatientsQuery } from '../hooks/usePatientsQuery';

export interface PatientSearchFilters {
  name: string;
  age: string;
  uhid: string;
  phone: string;
  /** Narrows results to patients still flagged with placeholder data from bulk import — the
   * one filter here that's a checkbox, not a text box, so it can stand alone as a search
   * ("browse everyone needing verification") without requiring a name/age/UHID/phone too. */
  needsVerification: boolean;
}

export const emptyPatientSearchFilters: PatientSearchFilters = { name: '', age: '', uhid: '', phone: '', needsVerification: false };

interface PatientListToolbarProps {
  filters: PatientSearchFilters;
  onFilterChange: (field: keyof PatientSearchFilters, value: string | boolean) => void;
  onSearch: () => void;
  onClear: () => void;
  /** When provided, Name/UHID/Phone show a live, type-ahead suggestions dropdown (debounced,
   * matching the backend's general OR-across-name/UHID/phone search — same param PatientPicker
   * already uses for its own single-field case) as the receptionist types, so a match can be
   * picked directly instead of always needing an explicit Search click first. Optional so a
   * bare consumer of this toolbar isn't forced into the extra query traffic/UI if it has no use
   * for picking a single patient (Age has no suggestions — it's a numeric range filter, not a
   * text prefix). */
  onSuggestionSelect?: (patient: Patient) => void;
}

type SuggestableField = 'name' | 'uhid' | 'phone';

const hasAnyFilter = (filters: PatientSearchFilters) =>
  filters.needsVerification || [filters.name, filters.age, filters.uhid, filters.phone].some((value) => value.trim() !== '');

/** Search by any one or a combination of Name / Age / UHID / Phone Number — a Search action is explicit, results aren't shown until one runs. */
export function PatientListToolbar({ filters, onFilterChange, onSearch, onClear, onSuggestionSelect }: PatientListToolbarProps) {
  const canSearch = hasAnyFilter(filters);
  const { hasPermission } = useAuth();

  const [activeField, setActiveField] = useState<SuggestableField | null>(null);
  const activeValue = activeField ? filters[activeField] : '';
  const debouncedValue = useDebouncedValue(activeValue, 250);
  const showSuggestions = Boolean(onSuggestionSelect) && activeField !== null && debouncedValue.trim().length > 0;

  const { data: suggestionData, isFetching: suggestionsFetching } = usePatientsQuery(
    { page: 1, pageSize: 8, sort: 'lastName', search: debouncedValue.trim() },
    { enabled: showSuggestions },
  );
  const suggestions = showSuggestions ? (suggestionData?.items ?? []) : [];

  function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    if (canSearch) onSearch();
  }

  function handleSuggestionClick(patient: Patient) {
    setActiveField(null);
    onSuggestionSelect?.(patient);
  }

  /** Wraps one Name/UHID/Phone Input with its own live-suggestions popover — a no-op pass-
   * through when onSuggestionSelect isn't supplied, so the field renders exactly as before. */
  function withSuggestions(field: SuggestableField, input: React.ReactNode) {
    if (!onSuggestionSelect) return input;
    const open = activeField === field && (showSuggestions || suggestionsFetching);

    return (
      <Popover open={open}>
        <PopoverAnchor asChild>{input}</PopoverAnchor>
        <PopoverContent
          align="start"
          className="w-[--radix-popover-trigger-width] p-1"
          // Both of these would otherwise steal keyboard focus away from the text input the
          // moment this popover opens/closes — the whole point here is that the input stays
          // focused and keeps receiving keystrokes the entire time this is open.
          onOpenAutoFocus={(event) => event.preventDefault()}
          onCloseAutoFocus={(event) => event.preventDefault()}
        >
          {suggestionsFetching ? (
            <p className="px-2 py-3 text-center text-sm text-muted-foreground">Searching…</p>
          ) : suggestions.length === 0 ? (
            <p className="px-2 py-3 text-center text-sm text-muted-foreground">No matching patients</p>
          ) : (
            <ul role="listbox" className="max-h-64 overflow-y-auto">
              {suggestions.map((patient) => (
                <li key={patient.id}>
                  <button
                    type="button"
                    role="option"
                    aria-selected={false}
                    // onMouseDown (not onClick) fires before the input's onBlur would — without
                    // preventDefault here, the input blurs (closing this popover) before the
                    // click below ever registers, so nothing would ever get selected.
                    onMouseDown={(event) => event.preventDefault()}
                    onClick={() => handleSuggestionClick(patient)}
                    className="flex w-full flex-col items-start gap-0.5 rounded-sm px-2 py-1.5 text-left text-sm hover:bg-accent hover:text-accent-foreground"
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
          )}
        </PopoverContent>
      </Popover>
    );
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-3 rounded-lg border border-border bg-card p-4 shadow-soft-md">
      <div className="flex flex-wrap items-end gap-3">
        <div className="flex min-w-[160px] flex-1 flex-col gap-1">
          <Label htmlFor="search-name">Patient Name</Label>
          {withSuggestions(
            'name',
            <Input
              id="search-name"
              placeholder="e.g. Karthik Selvam"
              value={filters.name}
              onChange={(event) => onFilterChange('name', event.target.value)}
              onFocus={() => setActiveField('name')}
              onBlur={() => setActiveField((current) => (current === 'name' ? null : current))}
              autoComplete="off"
            />,
          )}
        </div>
        <div className="flex w-full flex-col gap-1 sm:w-28">
          <Label htmlFor="search-age">Age</Label>
          <Input
            id="search-age"
            type="number"
            min={0}
            placeholder="e.g. 42"
            value={filters.age}
            onChange={(event) => onFilterChange('age', event.target.value)}
          />
        </div>
        <div className="flex w-full flex-col gap-1 sm:w-44">
          <Label htmlFor="search-uhid">UHID</Label>
          {withSuggestions(
            'uhid',
            <Input
              id="search-uhid"
              placeholder="e.g. NH20260018"
              value={filters.uhid}
              onChange={(event) => onFilterChange('uhid', event.target.value)}
              onFocus={() => setActiveField('uhid')}
              onBlur={() => setActiveField((current) => (current === 'uhid' ? null : current))}
              autoComplete="off"
            />,
          )}
        </div>
        <div className="flex w-full flex-col gap-1 sm:w-48">
          <Label htmlFor="search-phone">Phone Number</Label>
          {withSuggestions(
            'phone',
            <Input
              id="search-phone"
              type="tel"
              placeholder="e.g. 9600889012"
              value={filters.phone}
              onChange={(event) => onFilterChange('phone', event.target.value)}
              onFocus={() => setActiveField('phone')}
              onBlur={() => setActiveField((current) => (current === 'phone' ? null : current))}
              autoComplete="off"
            />,
          )}
        </div>

        <label className="flex items-center gap-2 pb-2 text-sm text-foreground">
          <input
            type="checkbox"
            checked={filters.needsVerification}
            onChange={(event) => onFilterChange('needsVerification', event.target.checked)}
            className="h-3.5 w-3.5 rounded border-input text-primary focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
          />
          Needs verification only
        </label>

        <div className="flex gap-2">
          <Button type="submit" disabled={!canSearch} className="gap-1.5">
            <Search className="h-4 w-4" />
            Search
          </Button>
          {hasAnyFilter(filters) && (
            <Button type="button" variant="outline" onClick={onClear} className="gap-1.5">
              <X className="h-4 w-4" />
              Clear
            </Button>
          )}
        </div>

        {hasPermission('patient-management.create') && (
          <Button asChild className="ml-auto gap-1.5">
            <Link to="/patients/registration/new">
              <Plus className="h-4 w-4" />
              New Patient
            </Link>
          </Button>
        )}
      </div>
    </form>
  );
}
