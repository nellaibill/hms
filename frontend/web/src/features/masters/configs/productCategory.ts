import { FolderTree } from 'lucide-react';
import type { MasterEntityConfig } from '../engine/types';

export const productCategoryConfig: MasterEntityConfig = {
  key: 'productCategory',
  label: 'Product Category',
  labelPlural: 'Product Categories',
  description: 'Top-level product classification (e.g. Pharmacy, Consumables, Equipment).',
  icon: FolderTree,
  section: 'Pharmacy & Inventory',
  codeField: 'categoryCode',
  nameField: 'categoryName',
  fields: [
    { key: 'categoryCode', label: 'Category Code', type: 'text', required: true, placeholder: 'e.g. PHARM' },
    { key: 'categoryName', label: 'Category Name', type: 'text', required: true },
    {
      key: 'parentId',
      label: 'Parent Category',
      type: 'reference',
      referenceEntityKey: 'productCategory',
      excludeSelf: true,
      helpText: 'Optional — leave unset for a top-level category.',
    },
    { key: 'sortOrder', label: 'Sort Order', type: 'number', defaultValue: 0, min: 0, step: 1 },
    { key: 'description', label: 'Description', type: 'textarea' },
  ],
};
