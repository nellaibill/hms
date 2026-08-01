import { ArrowLeft, Loader2, Package, Pencil } from 'lucide-react';
import { Link, useParams } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { ProductDetails, useProductQuery } from '@/features/products';

export default function ProductViewPage() {
  const { id } = useParams<{ id: string }>();
  const { data: product, isPending, isError } = useProductQuery(id);

  if (isPending) {
    return (
      <div className="flex flex-1 items-center justify-center gap-2 p-6 text-sm text-muted-foreground">
        <Loader2 className="h-4 w-4 animate-spin" />
        Loading product…
      </div>
    );
  }

  if (isError || !product) {
    return (
      <div className="p-6">
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          Product not found.
        </p>
      </div>
    );
  }

  return (
    <div className="flex flex-1 flex-col">
      <div className="px-6 pt-4 lg:px-8">
        <Link to="/support/inventory" className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground">
          <ArrowLeft className="h-4 w-4" />
          Back to products
        </Link>
      </div>

      <div className="relative mt-3 flex flex-col items-center gap-1 bg-page-banner px-6 py-5 text-center text-page-banner-foreground">
        <Button
          asChild
          variant="outline"
          className="absolute right-6 top-1/2 -translate-y-1/2 gap-1.5 border-page-banner-foreground/30 bg-page-banner-foreground/10 text-page-banner-foreground hover:bg-page-banner-foreground/20"
        >
          <Link to={`/support/inventory/${product.id}/edit`}>
            <Pencil className="h-4 w-4" />
            Edit
          </Link>
        </Button>
        <div className="flex items-center gap-3">
          <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-page-banner-foreground/15 text-page-banner-foreground">
            <Package className="h-5 w-5" />
          </span>
          <h1 className="text-xl font-semibold tracking-tight">{product.productName}</h1>
        </div>
        <p className="text-sm text-page-banner-foreground/85">{product.sku}</p>
      </div>

      <div className="flex flex-1 flex-col gap-6 p-6 lg:p-8">
        <ProductDetails product={product} />
      </div>
    </div>
  );
}
