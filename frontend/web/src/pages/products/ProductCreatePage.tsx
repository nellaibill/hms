import { ApiError, type ProductProfileFormValues } from '@hms/shared';
import { PackagePlus } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { PageBanner } from '@/components/PageBanner';
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
      <PageBanner
        icon={PackagePlus}
        title="New Product"
        subtitle="Add a new item to the Products catalog."
        backTo="/support/inventory"
        backLabel="Back to products"
      />

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
