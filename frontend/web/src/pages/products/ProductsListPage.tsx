import { Boxes, Loader2 } from 'lucide-react';
import { useState } from 'react';
import { Card, CardContent } from '@/components/ui/card';
import { Pagination, ProductListToolbar, ProductTable, useProductsQuery } from '@/features/products';
import { useDebouncedValue } from '../../hooks/useDebouncedValue';

export default function ProductsListPage() {
  const [search, setSearch] = useState('');
  const [isActive, setIsActive] = useState<boolean | undefined>(undefined);
  const [page, setPage] = useState(1);
  const [sort, setSort] = useState('-createdAt');

  const debouncedSearch = useDebouncedValue(search);

  const { data, isPending, isError, error } = useProductsQuery({
    page,
    pageSize: 20,
    sort,
    search: debouncedSearch || undefined,
    isActive,
  });

  function handleSearchChange(value: string) {
    setSearch(value);
    setPage(1);
  }

  function handleIsActiveChange(value: boolean | undefined) {
    setIsActive(value);
    setPage(1);
  }

  function handleSortChange(value: string) {
    setSort(value);
    setPage(1);
  }

  return (
    <div className="flex flex-1 flex-col">
      {/* Centered, brand-colored banner — matches the Page banner style used across module pages. */}
      <div className="flex flex-col items-center gap-1 bg-page-banner px-6 py-5 text-center text-page-banner-foreground">
        <div className="flex items-center gap-3">
          <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-page-banner-foreground/15 text-page-banner-foreground">
            <Boxes className="h-5 w-5" />
          </span>
          <h1 className="text-xl font-semibold tracking-tight">Products</h1>
        </div>
        <p className="max-w-2xl text-sm text-page-banner-foreground/85">
          Item master — the Products reference module, connected live to the HMS.Api backend.
        </p>
      </div>

      <div className="flex flex-1 flex-col gap-6 p-6 lg:p-8">
        <ProductListToolbar
          search={search}
          onSearchChange={handleSearchChange}
          isActive={isActive}
          onIsActiveChange={handleIsActiveChange}
        />

        {isPending && (
          <div className="flex items-center justify-center gap-2 py-16 text-sm text-muted-foreground">
            <Loader2 className="h-4 w-4 animate-spin" />
            Loading products…
          </div>
        )}

        {isError && (
          <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
            {error instanceof Error ? error.message : 'Failed to load products.'}
          </p>
        )}

        {!isPending && !isError && data && data.items.length === 0 && (
          <Card className="border-dashed">
            <CardContent className="flex flex-col items-center gap-2 py-16 text-center">
              <p className="text-sm font-medium text-foreground">No products found</p>
              <p className="text-sm text-muted-foreground">
                {debouncedSearch ? `No results for "${debouncedSearch}".` : 'Add the first product to get started.'}
              </p>
            </CardContent>
          </Card>
        )}

        {!isPending && !isError && data && data.items.length > 0 && (
          <div className="flex flex-col gap-3">
            <ProductTable products={data.items} sort={sort} onSortChange={handleSortChange} />
            <Pagination meta={data.meta} onPageChange={setPage} />
          </div>
        )}
      </div>
    </div>
  );
}
