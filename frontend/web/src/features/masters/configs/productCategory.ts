import { FolderTree } from 'lucide-react';
import type { MasterEntityConfig } from '../engine/types';

export const productCategoryConfig: MasterEntityConfig = {
  key: 'productCategory',
  label: 'Product Category',
  labelPlural: 'Product Categories',
  description: 'Top-level product classification (e.g. Pharmacy, Consumables, Equipment).',
  icon: FolderTree,
  section: 'Product Classification',
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
  seed: [
    { id: 'product-category-001', categoryCode: 'PHARM', categoryName: 'Pharmacy', sortOrder: 1, description: 'Drugs and pharmaceutical products.' },
    { id: 'product-category-002', categoryCode: 'CONSUM', categoryName: 'Consumables', sortOrder: 2, description: 'Single-use clinical consumables.' },
    { id: 'product-category-003', categoryCode: 'EQUIP', categoryName: 'Equipment', sortOrder: 3, description: 'Reusable medical and non-medical equipment.' },
    { id: 'product-category-004', categoryCode: 'SURGICAL', categoryName: 'Surgical Supplies', parentId: 'product-category-002', sortOrder: 1, description: 'Surgical consumables, a sub-set of Consumables.' },
  ],
};
