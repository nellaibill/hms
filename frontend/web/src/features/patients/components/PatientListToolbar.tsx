import { Plus, Search, X } from 'lucide-react';
import { Link } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { useAuth } from '../../auth/AuthContext';

export interface PatientSearchFilters {
  name: string;
  age: string;
  uhid: string;
  phone: string;
}

export const emptyPatientSearchFilters: PatientSearchFilters = { name: '', age: '', uhid: '', phone: '' };

interface PatientListToolbarProps {
  filters: PatientSearchFilters;
  onFilterChange: (field: keyof PatientSearchFilters, value: string) => void;
  onSearch: () => void;
  onClear: () => void;
}

const hasAnyFilter = (filters: PatientSearchFilters) => Object.values(filters).some((value) => value.trim() !== '');

/** Search by any one or a combination of Name / Age / UHID / Phone Number — a Search action is explicit, results aren't shown until one runs. */
export function PatientListToolbar({ filters, onFilterChange, onSearch, onClear }: PatientListToolbarProps) {
  const canSearch = hasAnyFilter(filters);
  const { hasPermission } = useAuth();

  function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    if (canSearch) onSearch();
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-3 rounded-lg border border-border bg-card p-4 shadow-soft-md">
      <div className="flex flex-wrap items-end gap-3">
        <div className="flex min-w-[160px] flex-1 flex-col gap-1">
          <Label htmlFor="search-name">Patient Name</Label>
          <Input
            id="search-name"
            placeholder="e.g. Karthik Selvam"
            value={filters.name}
            onChange={(event) => onFilterChange('name', event.target.value)}
          />
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
          <Input
            id="search-uhid"
            placeholder="e.g. NH20260018"
            value={filters.uhid}
            onChange={(event) => onFilterChange('uhid', event.target.value)}
          />
        </div>
        <div className="flex w-full flex-col gap-1 sm:w-48">
          <Label htmlFor="search-phone">Phone Number</Label>
          <Input
            id="search-phone"
            type="tel"
            placeholder="e.g. 9600889012"
            value={filters.phone}
            onChange={(event) => onFilterChange('phone', event.target.value)}
          />
        </div>

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
