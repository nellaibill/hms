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
};
