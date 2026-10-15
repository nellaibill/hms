import type { Patient } from '@hms/shared';
import { Loader2, Search } from 'lucide-react';
import { useState } from 'react';
import { Input } from '@/components/ui/input';
import { usePatientsQuery } from '@/features/patients/hooks/usePatientsQuery';
import { useDebouncedValue } from '@/hooks/useDebouncedValue';

interface PatientQuickSearchProps {
  onSelect: (patient: Patient) => void;
  /** Offered when nothing matches, e.g. "Book as a new caller". */
  noMatchAction?: { label: string; onClick: () => void };
  initialSearch?: string;
}

/** A one-box patient search (name, UHID or phone) with up to six matches — small enough for a
 * dialog, unlike Billing's full PatientPicker. */
export function PatientQuickSearch({ onSelect, noMatchAction, initialSearch = '' }: PatientQuickSearchProps) {
  const [search, setSearch] = useState(initialSearch);
  const debouncedSearch = useDebouncedValue(search.trim());
  const { data: matches, isFetching } = usePatientsQuery({ page: 1, pageSize: 6, search: debouncedSearch, sort: 'lastName' }, { enabled: debouncedSearch.length >= 2 });

  return (
    <div className="flex flex-col gap-2">
      <div className="relative">
        <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
        <Input
          autoFocus
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          placeholder="Search by name, UHID or phone…"
          aria-label="Search patients"
          className="pl-9"
        />
        {isFetching && <Loader2 className="absolute right-3 top-1/2 h-4 w-4 -translate-y-1/2 animate-spin text-muted-foreground" />}
      </div>
      {debouncedSearch.length >= 2 && matches && (
        <ul className="divide-y divide-border overflow-hidden rounded-md border border-border">
          {matches.items.length === 0 && (
            <li className="px-3 py-3 text-sm text-muted-foreground">
              No registered patient matches.
              {noMatchAction && (
                <>
                  {' '}
                  <button type="button" className="font-medium text-link hover:underline" onClick={noMatchAction.onClick}>
                    {noMatchAction.label}
                  </button>
                </>
              )}
            </li>
          )}
          {matches.items.map((match) => (
            <li key={match.id}>
              <button
                type="button"
                onClick={() => onSelect(match)}
                className="flex w-full items-center justify-between gap-3 px-3 py-2 text-left text-sm hover:bg-accent"
              >
                <span className="font-medium text-foreground">
                  {match.title} {match.firstName} {match.lastName}
                  <span className="ml-2 text-xs font-normal text-muted-foreground">
                    {match.age} y / {match.gender[0]}
                  </span>
                </span>
                <span className="flex shrink-0 items-center gap-3 text-xs text-muted-foreground">
                  <span className="font-mono">{match.uhid}</span>
                  <span>{match.primaryPhone}</span>
                </span>
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
