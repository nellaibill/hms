import type { Product } from '@hms/shared';
import { Pill } from 'lucide-react';
import { useMemo, useState } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { useProductCategoriesQuery } from '@/features/pharmacy/product-lookup';

interface QuickPickPanelProps {
  products: Product[];
  selectedProductId: string;
  onSelectProduct: (productId: string) => void;
}

const ALL_TAB = 'all';

/**
 * A faster way to fill the Add Item card's Product field — nothing more. Clicking a tile
 * calls the exact same onSelectProduct the Product dropdown's onValueChange calls, so Batch
 * selection, quantity entry, and stock/expiry validation all stay in the one existing Add
 * Item flow; this panel owns no cart or dispense logic of its own.
 *
 * Tabs are built from the categories actually present among the passed-in products — a
 * category with zero products here would be a tab a pharmacist can click into and find
 * nothing, so it's left out rather than listing every category master record. Uses the same
 * underlined Tabs primitive as the rest of the app (e.g. patient registration) rather than a
 * one-off pill style, so this reads as part of the same product, not a bolted-on widget.
 */
export function QuickPickPanel({ products, selectedProductId, onSelectProduct }: QuickPickPanelProps) {
  const categoriesQuery = useProductCategoriesQuery();
  const [activeTab, setActiveTab] = useState(ALL_TAB);

  const categoryTabs = useMemo(() => {
    const categoriesInUse = new Set(products.map((p) => p.categoryId));
    return (categoriesQuery.data ?? []).filter((c) => categoriesInUse.has(c.id));
  }, [products, categoriesQuery.data]);

  const visibleProducts = activeTab === ALL_TAB ? products : products.filter((p) => p.categoryId === activeTab);

  return (
    <Card>
      <CardHeader className="flex-row items-center gap-3 space-y-0 p-4">
        <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-primary/10 text-primary">
          <Pill className="h-4.5 w-4.5" />
        </span>
        <CardTitle className="text-base">Quick Pick</CardTitle>
      </CardHeader>
      <CardContent className="flex flex-col gap-3 p-4 pt-0">
        <Tabs value={activeTab} onValueChange={setActiveTab}>
          <TabsList>
            <TabsTrigger value={ALL_TAB}>All</TabsTrigger>
            {categoryTabs.map((category) => (
              <TabsTrigger key={category.id} value={category.id}>
                {category.categoryName}
              </TabsTrigger>
            ))}
          </TabsList>
        </Tabs>

        {visibleProducts.length === 0 ? (
          <p className="py-6 text-center text-sm text-muted-foreground">No products in this category.</p>
        ) : (
          <div className="grid grid-cols-2 gap-2.5 sm:grid-cols-3 md:grid-cols-4">
            {visibleProducts.map((product) => {
              const isSelected = product.id === selectedProductId;
              return (
                <button
                  key={product.id}
                  type="button"
                  onClick={() => onSelectProduct(product.id)}
                  className={`flex flex-col items-center gap-1.5 rounded-lg border p-3 text-center shadow-soft transition-all ${
                    isSelected
                      ? 'border-primary bg-primary/5 shadow-soft-md'
                      : 'border-border hover:-translate-y-0.5 hover:border-primary/40 hover:shadow-soft-md'
                  }`}
                >
                  <span className="flex h-10 w-10 items-center justify-center rounded-full bg-primary/10 text-primary">
                    <Pill className="h-5 w-5" />
                  </span>
                  <span className="line-clamp-2 text-xs font-medium leading-snug text-foreground">{product.productName}</span>
                  <span className="rounded-full bg-primary/10 px-2 py-0.5 text-[11px] font-semibold text-primary">
                    ₹{product.sellingPrice.toFixed(2)}
                  </span>
                </button>
              );
            })}
          </div>
        )}
      </CardContent>
    </Card>
  );
}
