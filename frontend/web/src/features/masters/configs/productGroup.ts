import { FolderTree } from 'lucide-react';
import type { MasterEntityConfig } from '../engine/types';

export const productGroupConfig: MasterEntityConfig = {
  key: 'productGroup',
  label: 'Product Group',
  labelPlural: 'Product Groups',
  description: 'Third-level classification, each belonging to one Product Sub Category.',
  icon: FolderTree,
  section: 'Pharmacy & Inventory',
  codeField: 'groupCode',
  nameField: 'groupName',
  fields: [
    { key: 'groupCode', label: 'Group Code', type: 'text', required: true },
    { key: 'groupName', label: 'Group Name', type: 'text', required: true },
    { key: 'subCategoryId', label: 'Sub Category', type: 'reference', referenceEntityKey: 'productSubCategory', required: true },
    { key: 'sortOrder', label: 'Sort Order', type: 'number', defaultValue: 0, min: 0, step: 1 },
    { key: 'description', label: 'Description', type: 'textarea' },
  ],
};
