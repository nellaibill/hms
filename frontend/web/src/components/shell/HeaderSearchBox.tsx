import { useState } from 'react';
import { Search } from 'lucide-react';
import { Input } from '@/components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';

type SearchScope = 'patient' | 'doctor';

const SEARCH_PLACEHOLDER = 'Search by Name, UHID, or Phone…';

/** Single unified control — a scope selector (Patient/Doctor) and a text input sharing one bordered box. */
export function HeaderSearchBox() {
  const [scope, setScope] = useState<SearchScope>('patient');

  return (
    <div className="flex w-full items-stretch overflow-hidden rounded-md border border-input bg-background shadow-sm">
      <Select value={scope} onValueChange={(value) => setScope(value as SearchScope)}>
        <SelectTrigger
          aria-label="Search scope"
          className="w-[9.5rem] shrink-0 rounded-none border-0 border-r border-input bg-transparent shadow-none focus:ring-0 focus:ring-offset-0"
        >
          <SelectValue />
        </SelectTrigger>
        <SelectContent>
          <SelectItem value="patient">Patient Search</SelectItem>
          <SelectItem value="doctor">Doctor Search</SelectItem>
        </SelectContent>
      </Select>
      <div className="relative flex-1">
        <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
        <Input
          type="search"
          placeholder={SEARCH_PLACEHOLDER}
          aria-label={scope === 'patient' ? 'Patient search' : 'Doctor search'}
          className="h-full rounded-none border-0 pl-9 shadow-none focus-visible:ring-0 focus-visible:ring-offset-0"
        />
      </div>
    </div>
  );
}
