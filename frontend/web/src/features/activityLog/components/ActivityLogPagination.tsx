import type { PaginationMeta } from '@hms/shared';
import { ChevronLeft, ChevronRight } from 'lucide-react';
import { Button } from '@/components/ui/button';

interface ActivityLogPaginationProps {
  meta: PaginationMeta;
  onPageChange: (page: number) => void;
}

/** Current page ±1, plus first and last, with "…" gaps — e.g. 1 … 4 5 6 … 25. */
function visiblePages(page: number, totalPages: number): (number | 'gap')[] {
  const wanted = new Set([1, totalPages, page - 1, page, page + 1].filter((p) => p >= 1 && p <= totalPages));
  const sorted = [...wanted].sort((a, b) => a - b);
  const out: (number | 'gap')[] = [];
  sorted.forEach((p, i) => {
    if (i > 0 && p - sorted[i - 1] > 1) {
      out.push('gap');
    }
    out.push(p);
  });
  return out;
}

export function ActivityLogPagination({ meta, onPageChange }: ActivityLogPaginationProps) {
  const first = meta.totalCount === 0 ? 0 : (meta.page - 1) * meta.pageSize + 1;
  const last = Math.min(meta.page * meta.pageSize, meta.totalCount);

  return (
    <div className="flex flex-wrap items-center justify-between gap-2">
      <p className="text-sm text-muted-foreground">
        Showing {first} – {last} of {meta.totalCount} results
      </p>
      {meta.totalPages > 1 && (
        <nav className="flex items-center gap-1" aria-label="Pagination">
          <Button variant="outline" size="icon" className="h-8 w-8" disabled={meta.page <= 1} onClick={() => onPageChange(meta.page - 1)} aria-label="Previous page">
            <ChevronLeft className="h-4 w-4" />
          </Button>
          {visiblePages(meta.page, meta.totalPages).map((p, i) =>
            p === 'gap' ? (
              <span key={`gap-${i}`} className="px-1 text-sm text-muted-foreground">
                …
              </span>
            ) : (
              <Button
                key={p}
                variant={p === meta.page ? 'default' : 'outline'}
                size="icon"
                className="h-8 w-8"
                aria-current={p === meta.page ? 'page' : undefined}
                onClick={() => onPageChange(p)}
              >
                {p}
              </Button>
            ),
          )}
          <Button variant="outline" size="icon" className="h-8 w-8" disabled={meta.page >= meta.totalPages} onClick={() => onPageChange(meta.page + 1)} aria-label="Next page">
            <ChevronRight className="h-4 w-4" />
          </Button>
        </nav>
      )}
    </div>
  );
}
