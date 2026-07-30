import { FolderTree } from 'lucide-react';
import type { MasterEntityConfig } from '../engine/types';

export const productSubCategoryConfig: MasterEntityConfig = {
  key: 'productSubCategory',
  label: 'Product Sub Category',
  labelPlural: 'Product Sub Categories',
  description: 'Second-level classification, each belonging to one Product Category.',
  icon: FolderTree,
  section: 'Product Classification',
  codeField: 'subCategoryCode',
  nameField: 'subCategoryName',
  fields: [
    { key: 'subCategoryCode', label: 'Sub Category Code', type: 'text', required: true },
    { key: 'subCategoryName', label: 'Sub Category Name', type: 'text', required: true },
    { key: 'categoryId', label: 'Category', type: 'reference', referenceEntityKey: 'productCategory', required: true },
    { key: 'sortOrder', label: 'Sort Order', type: 'number', defaultValue: 0, min: 0, step: 1 },
    { key: 'description', label: 'Description', type: 'textarea' },
  ],
  seed: [
    { id: 'product-sub-category-001', subCategoryCode: 'PHARM-TAB', subCategoryName: 'Tablets & Capsules', categoryId: 'product-category-001', sortOrder: 1 },
    { id: 'product-sub-category-002', subCategoryCode: 'PHARM-INJ', subCategoryName: 'Injectables', categoryId: 'product-category-001', sortOrder: 2 },
    { id: 'product-sub-category-003', subCategoryCode: 'CONSUM-PPE', subCategoryName: 'PPE', categoryId: 'product-category-002', sortOrder: 1 },
    { id: 'product-sub-category-004', subCategoryCode: 'EQUIP-DIAG', subCategoryName: 'Diagnostic Equipment', categoryId: 'product-category-003', sortOrder: 1 },
  ],
};
