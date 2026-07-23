/** Mirrors HMS.Shared.Kernel.PagedResult<T> — see docs/ApiStandards.md §6. */
export interface PaginationMeta {
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface PagedQuery {
  page?: number;
  pageSize?: number;
  sort?: string;
  search?: string;
}
