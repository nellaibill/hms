import type { PaginationMeta } from '@hms/shared';

interface PaginationProps {
  meta: PaginationMeta;
  onPageChange: (page: number) => void;
}

export function Pagination({ meta, onPageChange }: PaginationProps) {
  if (meta.totalPages <= 1) {
    return null;
  }

  return (
    <div className="pagination">
      <button type="button" disabled={meta.page <= 1} onClick={() => onPageChange(meta.page - 1)}>
        Previous
      </button>
      <span>
        Page {meta.page} of {meta.totalPages} ({meta.totalCount} total)
      </span>
      <button type="button" disabled={meta.page >= meta.totalPages} onClick={() => onPageChange(meta.page + 1)}>
        Next
      </button>
    </div>
  );
}
