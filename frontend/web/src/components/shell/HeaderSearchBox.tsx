import type { FormEvent } from 'react';
import { useState } from 'react';
import type { Patient } from '@hms/shared';
import { Search } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { Input } from '@/components/ui/input';
import { Popover, PopoverAnchor, PopoverContent } from '@/components/ui/popover';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { useDebouncedValue } from '@/hooks/useDebouncedValue';
import { usePatientsQuery } from '@/features/patients/hooks/usePatientsQuery';

type SearchScope = 'patient' | 'doctor';

const SEARCH_PLACEHOLDER = 'Search by Name, UHID, or Phone…';

/** Single unified control — a scope selector (Patient/Doctor) and a text input sharing one bordered box.
 * Patient scope is wired to live data: typing shows a debounced type-ahead dropdown (the same
 * OR-across-name/UHID/phone match PatientListToolbar's own suggestions use), and picking a result
 * jumps straight to that patient's record. Pressing Enter without picking one runs the same text
 * as a full search on the Patient Enquiry page. Doctor scope has no backing search API yet, so it
 * stays a plain input for now. */
export function HeaderSearchBox() {
  const navigate = useNavigate();
  const [scope, setScope] = useState<SearchScope>('patient');
  const [query, setQuery] = useState('');
  const [isFocused, setIsFocused] = useState(false);

  const debouncedQuery = useDebouncedValue(query, 250);
  const trimmedQuery = debouncedQuery.trim();
  const showSuggestions = scope === 'patient' && isFocused && trimmedQuery.length > 0;

  const { data, isFetching } = usePatientsQuery(
    { page: 1, pageSize: 8, sort: 'lastName', search: trimmedQuery },
    { enabled: showSuggestions },
  );
  const suggestions = showSuggestions ? (data?.items ?? []) : [];

  function goToPatient(patient: Patient) {
    setQuery('');
    setIsFocused(false);
    navigate(`/patients/registration/${patient.id}`);
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
          className="w-[9.5rem] shrink-0 rounded-none border-0 border-r border-slate-200 bg-transparent text-slate-700 shadow-none focus:ring-0 focus:ring-offset-0"
        >
          <SelectValue />
        </SelectTrigger>
        <SelectContent>
          <SelectItem value="patient">Patient Search</SelectItem>
          <SelectItem value="doctor">Doctor Search</SelectItem>
        </SelectContent>
      </Select>
      <div className="relative flex-1">
        <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
        <Popover open={showSuggestions}>
          <PopoverAnchor asChild>
            <Input
              type="search"
              placeholder={SEARCH_PLACEHOLDER}
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
                      onClick={() => goToPatient(patient)}
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
      </div>
    </form>
  );
}
