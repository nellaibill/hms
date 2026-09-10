import { ApiError, type ProductProfileFormValues } from '@hms/shared';
import { Loader2, Settings2 } from 'lucide-react';
import { useNavigate, useParams } from 'react-router-dom';
import { PageBanner } from '@/components/PageBanner';
import { ProductForm, useProductQuery, useUpdateProductMutation } from '@/features/products';
import { RequirePermission } from '@/features/auth/RequirePermission';

export default function ProductEditPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { data: product, isPending, isError } = useProductQuery(id);
  const mutation = useUpdateProductMutation();

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

  function handleSubmit(values: ProductProfileFormValues) {
    mutation.mutate(
      {
        id: id as string,
        request: {
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
      },
      {
        onSuccess: () => navigate(`/support/inventory/${id}`),
      },
    );
  }

  return (
    <RequirePermission permission="support-services.edit">
    <div className="flex flex-1 flex-col">
      <PageBanner
        icon={Settings2}
        title={`Edit ${product.productName}`}
        subtitle="Update this product's details."
        backTo={`/support/inventory/${id}`}
        backLabel="Back to product"
      />

      <div className="flex flex-1 flex-col gap-6 p-6 lg:p-8">
        <ProductForm
          mode="edit"
          submitLabel="Save Changes"
          isSubmitting={mutation.isPending}
          apiError={mutation.error instanceof ApiError ? mutation.error : null}
          defaultValues={{
            sku: product.sku,
            productCode: product.productCode,
            productName: product.productName,
            genericName: product.genericName ?? '',
            description: product.description ?? '',
            brandId: product.brandId,
            manufacturerId: product.manufacturerId,
            categoryId: product.categoryId,
            subCategoryId: product.subCategoryId,
            groupId: product.groupId,
            uomId: product.uomId,
            baseUomId: product.baseUomId,
            isBatchTracked: product.isBatchTracked,
            isSerialized: product.isSerialized,
            isActive: product.isActive,
            reorderLevel: product.reorderLevel,
            minStockLevel: product.minStockLevel,
            maxStockLevel: product.maxStockLevel,
            mrp: product.mrp,
            costPrice: product.costPrice,
            sellingPrice: product.sellingPrice,
            hsnCode: product.hsnCode ?? '',
            weight: product.weight ?? undefined,
            volume: product.volume ?? undefined,
          }}
          onSubmit={handleSubmit}
        />
      </div>
    </div>
    </RequirePermission>
  );
}
