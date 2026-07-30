import type { PaginationMeta } from '../engine/types';
import { Button } from '@/components/ui/button';

interface PaginationProps {
  meta: PaginationMeta;
  onPageChange: (page: number) => void;
}

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
