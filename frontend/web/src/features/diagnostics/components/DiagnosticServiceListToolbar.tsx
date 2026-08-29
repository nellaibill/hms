import type { DiagnosticCategory, DiagnosticServiceType } from '@hms/shared';
import { Plus } from 'lucide-react';
import { Link } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { useAuth } from '@/features/auth/AuthContext';

export interface DiagnosticServiceFilters {
  categoryId: string | undefined;
  serviceType: DiagnosticServiceType | undefined;
  isActive: boolean | undefined;
}

interface DiagnosticServiceListToolbarProps {
  filters: DiagnosticServiceFilters;
  onChange: (filters: DiagnosticServiceFilters) => void;
  categories: DiagnosticCategory[];
}

/** Mirrors the mockup's "All Categories" / "All Types" / "All Status" filter row. */
export function DiagnosticServiceListToolbar({ filters, onChange, categories }: DiagnosticServiceListToolbarProps) {
  const { hasPermission } = useAuth();

  return (
    <div className="flex flex-wrap items-center gap-3">
      <Select
        value={filters.categoryId ?? 'all'}
        onValueChange={(value) => onChange({ ...filters, categoryId: value === 'all' ? undefined : value })}
      >
        <SelectTrigger className="w-48" aria-label="Filter by category">
          <SelectValue placeholder="All Categories" />
        </SelectTrigger>
        <SelectContent>
          <SelectItem value="all">All Categories</SelectItem>
          {categories.map((category) => (
            <SelectItem key={category.id} value={category.id}>
              {category.name}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>

      <Select
        value={filters.serviceType ?? 'all'}
        onValueChange={(value) => onChange({ ...filters, serviceType: value === 'all' ? undefined : (value as DiagnosticServiceType) })}
      >
        <SelectTrigger className="w-40" aria-label="Filter by type">
          <SelectValue placeholder="All Types" />
        </SelectTrigger>
        <SelectContent>
          <SelectItem value="all">All Types</SelectItem>
          <SelectItem value="Laboratory">Laboratory</SelectItem>
          <SelectItem value="Radiology">Radiology</SelectItem>
        </SelectContent>
      </Select>

      <Select
        value={filters.isActive === undefined ? 'all' : String(filters.isActive)}
        onValueChange={(value) => onChange({ ...filters, isActive: value === 'all' ? undefined : value === 'true' })}
      >
        <SelectTrigger className="w-40" aria-label="Filter by status">
          <SelectValue placeholder="All Status" />
        </SelectTrigger>
        <SelectContent>
          <SelectItem value="all">All Status</SelectItem>
          <SelectItem value="true">Active only</SelectItem>
          <SelectItem value="false">Inactive only</SelectItem>
        </SelectContent>
      </Select>

      {hasPermission('diagnostics.create') && (
        <Button asChild className="ml-auto gap-1.5">
          <Link to="/diagnostics/lab/services/new">
            <Plus className="h-4 w-4" />
            Add Service
          </Link>
        </Button>
      )}
    </div>
  );
}
