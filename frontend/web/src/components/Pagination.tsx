import type { PaginationMeta } from '@hms/shared';
import { Button } from '@/components/ui/button';

interface PaginationProps {
  meta: PaginationMeta;
  onPageChange: (page: number) => void;
}

/**
 * App-wide pagination control. Users/Roles/Billing each carry their own copy of this exact
 * component predating this module; rather than adding a fourth+ duplicate, the HR feature
 * areas (Shifts, Staff Availability, Weekly Rosters, Shift Assignments, Shift Swap Requests)
 * share this one. Existing per-feature copies are left as-is (out of scope here).
 */
export function Pagination({ meta, onPageChange }: PaginationProps) {
  if (meta.totalPages <= 1) {
    return null;
  }

  return (
    <div className="flex items-center justify-between pt-1">
      <p className="text-sm text-muted-foreground">
        Page {meta.page} of {meta.totalPages} ({meta.totalCount} total)
      </p>
      <div className="flex gap-2">
        <Button variant="outline" size="sm" disabled={meta.page <= 1} onClick={() => onPageChange(meta.page - 1)}>
          Previous
        </Button>
        <Button variant="outline" size="sm" disabled={meta.page >= meta.totalPages} onClick={() => onPageChange(meta.page + 1)}>
          Next
        </Button>
      </div>
    </div>
  );
}
