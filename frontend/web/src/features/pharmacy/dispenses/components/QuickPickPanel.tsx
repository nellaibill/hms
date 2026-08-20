import type { Product } from '@hms/shared';
import { Pill } from 'lucide-react';
import { useMemo, useState } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
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
 * nothing, so it's left out rather than listing every category master record.
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
        <div className="flex flex-wrap gap-1.5 border-b border-border pb-3">
          <button type="button" onClick={() => setActiveTab(ALL_TAB)} className={tabClassName(activeTab === ALL_TAB)}>
            All
          </button>
          {categoryTabs.map((category) => (
            <button
              key={category.id}
              type="button"
              onClick={() => setActiveTab(category.id)}
              className={tabClassName(activeTab === category.id)}
            >
              {category.categoryName}
            </button>
          ))}
        </div>

        {visibleProducts.length === 0 ? (
          <p className="py-6 text-center text-sm text-muted-foreground">No products in this category.</p>
        ) : (
          <div className="grid grid-cols-2 gap-2 sm:grid-cols-3 md:grid-cols-4">
            {visibleProducts.map((product) => {
              const isSelected = product.id === selectedProductId;
              return (
                <button
                  key={product.id}
                  type="button"
                  onClick={() => onSelectProduct(product.id)}
                  className={`flex flex-col items-start gap-1 rounded-md border p-2.5 text-left transition-colors ${
                    isSelected ? 'border-primary bg-primary/5' : 'border-border hover:border-primary/40 hover:bg-accent/40'
                  }`}
                >
                  <span className="flex h-8 w-8 items-center justify-center rounded-md bg-muted text-muted-foreground">
                    <Pill className="h-4 w-4" />
                  </span>
                  <span className="line-clamp-2 text-xs font-medium text-foreground">{product.productName}</span>
                  <span className="text-xs text-muted-foreground">₹{product.sellingPrice.toFixed(2)}</span>
                </button>
              );
            })}
          </div>
        )}
      </CardContent>
    </Card>
  );
}

function tabClassName(isActive: boolean) {
  return `shrink-0 rounded-md px-2.5 py-1 text-xs font-medium transition-colors ${
    isActive ? 'bg-primary text-primary-foreground' : 'text-muted-foreground hover:bg-accent hover:text-foreground'
  }`;
}
