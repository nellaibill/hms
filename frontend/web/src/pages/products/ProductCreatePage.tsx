import { ApiError, type ProductProfileFormValues } from '@hms/shared';
import { ArrowLeft, PackagePlus } from 'lucide-react';
import { Link, useNavigate } from 'react-router-dom';
import { ProductForm, useCreateProductMutation } from '@/features/products';
import { RequirePermission } from '@/features/auth/RequirePermission';

export default function ProductCreatePage() {
  const navigate = useNavigate();
  const mutation = useCreateProductMutation();

  function handleSubmit(values: ProductProfileFormValues) {
    mutation.mutate(
      {
        sku: values.sku,
        productCode: values.productCode,
        productName: values.productName,
        genericName: values.genericName || undefined,
        description: values.description || undefined,
        brandId: values.brandId,
        manufacturerId: values.manufacturerId,
        categoryId: values.categoryId,
        subCategoryId: values.subCategoryId,
        groupId: values.groupId,
        uomId: values.uomId,
        baseUomId: values.baseUomId,
        isBatchTracked: values.isBatchTracked,
        isSerialized: values.isSerialized,
        isActive: values.isActive,
        reorderLevel: values.reorderLevel,
        minStockLevel: values.minStockLevel,
        maxStockLevel: values.maxStockLevel,
        mrp: values.mrp,
        costPrice: values.costPrice,
        sellingPrice: values.sellingPrice,
        hsnCode: values.hsnCode || undefined,
        weight: values.weight,
        volume: values.volume,
      },
      {
        onSuccess: (product) => navigate(`/support/inventory/${product.id}`),
      },
    );
  }

  return (
    <RequirePermission permission="support-services.create">
    <div className="flex flex-1 flex-col">
      <div className="px-6 pt-4 lg:px-8">
        <Link to="/support/inventory" className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground">
          <ArrowLeft className="h-4 w-4" />
          Back to products
        </Link>
      </div>

      <div className="mt-3 flex flex-col items-center gap-1 bg-page-banner px-6 py-5 text-center text-page-banner-foreground">
        <div className="flex items-center gap-3">
          <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-page-banner-foreground/15 text-page-banner-foreground">
            <PackagePlus className="h-5 w-5" />
          </span>
          <h1 className="text-xl font-semibold tracking-tight">New Product</h1>
        </div>
        <p className="text-sm text-page-banner-foreground/85">Add a new item to the Products catalog.</p>
      </div>

      <div className="flex flex-1 flex-col gap-6 p-6 lg:p-8">
        <ProductForm
          mode="create"
          submitLabel="Create Product"
          isSubmitting={mutation.isPending}
          apiError={mutation.error instanceof ApiError ? mutation.error : null}
          onSubmit={handleSubmit}
        />
      </div>
    </div>
    </RequirePermission>
  );
}
