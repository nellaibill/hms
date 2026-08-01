import type { Product } from '@hms/shared';
import { resolveRecordLabel, useMasterOptionsQuery } from '@/features/masters';
import { StatusBadge } from './StatusBadge';

interface ProductDetailsProps {
  product: Product;
}

function Field({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div className="flex flex-col gap-0.5 py-3">
      <dt className="text-xs font-medium uppercase tracking-wide text-muted-foreground">{label}</dt>
      <dd className="text-sm text-foreground">{value}</dd>
    </div>
  );
}

export function ProductDetails({ product }: ProductDetailsProps) {
  // Primes the Masters reference cache for every entity this view resolves labels from
  // (see engine/registry.ts's resolveRecordLabel).
  useMasterOptionsQuery('brand');
  useMasterOptionsQuery('manufacturer');
  useMasterOptionsQuery('productCategory');
  useMasterOptionsQuery('productSubCategory');
  useMasterOptionsQuery('productGroup');
  useMasterOptionsQuery('unitOfMeasure');

  return (
    <div className="flex flex-col gap-6">
      <dl className="grid grid-cols-1 divide-y divide-border rounded-lg border border-border px-4 sm:grid-cols-2 sm:divide-y-0 sm:divide-x sm:px-0 sm:[&>*]:px-6">
        <Field label="SKU" value={product.sku} />
        <Field label="Product Code" value={product.productCode} />
        <Field label="Generic Name" value={product.genericName || '—'} />
        <Field label="Description" value={product.description || '—'} />
        <Field label="Brand" value={resolveRecordLabel('brand', product.brandId)} />
        <Field label="Manufacturer" value={resolveRecordLabel('manufacturer', product.manufacturerId)} />
        <Field label="Category" value={resolveRecordLabel('productCategory', product.categoryId)} />
        <Field label="Sub-Category" value={resolveRecordLabel('productSubCategory', product.subCategoryId)} />
        <Field label="Group" value={resolveRecordLabel('productGroup', product.groupId)} />
        <Field label="Selling Unit" value={resolveRecordLabel('unitOfMeasure', product.uomId)} />
        <Field label="Base Unit" value={resolveRecordLabel('unitOfMeasure', product.baseUomId)} />
        <Field label="Batch Tracked" value={product.isBatchTracked ? 'Yes' : 'No'} />
        <Field label="Serialized" value={product.isSerialized ? 'Yes' : 'No'} />
        <Field label="Reorder Level" value={product.reorderLevel} />
        <Field label="Min Stock Level" value={product.minStockLevel} />
        <Field label="Max Stock Level" value={product.maxStockLevel} />
        <Field label="MRP" value={product.mrp.toLocaleString('en-IN')} />
        <Field label="Cost Price" value={product.costPrice.toLocaleString('en-IN')} />
        <Field label="Selling Price" value={product.sellingPrice.toLocaleString('en-IN')} />
        <Field label="HSN Code" value={product.hsnCode || '—'} />
        <Field label="Weight" value={product.weight ?? '—'} />
        <Field label="Volume" value={product.volume ?? '—'} />
        <Field label="Status" value={<StatusBadge isActive={product.isActive} />} />
        <Field label="Created" value={new Date(product.createdAt).toLocaleString('en-IN')} />
        {product.updatedAt && <Field label="Last updated" value={new Date(product.updatedAt).toLocaleString('en-IN')} />}
      </dl>

      <div className="rounded-lg border border-dashed border-border p-6 text-center">
        <p className="text-sm font-medium text-foreground">Batches, barcodes, images, prices &amp; tax mappings</p>
        <p className="mt-1 text-sm text-muted-foreground">
          Managing this product's batches, barcodes, images, price history, and tax mappings is coming in a future update.
        </p>
      </div>
    </div>
  );
}
