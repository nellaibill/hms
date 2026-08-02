export interface PaginationMeta {
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

/** Slices an array for client-side pagination and computes the same meta shape the async list hooks use, since report rows are already fully in memory (see incomeExpenseReport.ts). */
export function paginate<T>(items: T[], page: number, pageSize: number): { items: T[]; meta: PaginationMeta } {
  const totalCount = items.length;
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));
  const start = (page - 1) * pageSize;
  return {
    items: items.slice(start, start + pageSize),
    meta: { page, pageSize, totalCount, totalPages },
  };
}
