import { FolderTree } from 'lucide-react';
import type { MasterEntityConfig } from '../engine/types';

export const productGroupConfig: MasterEntityConfig = {
  key: 'productGroup',
  label: 'Product Group',
  labelPlural: 'Product Groups',
  description: 'Third-level classification, each belonging to one Product Sub Category.',
  icon: FolderTree,
  section: 'Product Classification',
  codeField: 'groupCode',
  nameField: 'groupName',
  fields: [
    { key: 'groupCode', label: 'Group Code', type: 'text', required: true },
    { key: 'groupName', label: 'Group Name', type: 'text', required: true },
    { key: 'subCategoryId', label: 'Sub Category', type: 'reference', referenceEntityKey: 'productSubCategory', required: true },
    { key: 'sortOrder', label: 'Sort Order', type: 'number', defaultValue: 0, min: 0, step: 1 },
    { key: 'description', label: 'Description', type: 'textarea' },
  ],
  seed: [
    { id: 'product-group-001', groupCode: 'TAB-ANALGESIC', groupName: 'Analgesics', subCategoryId: 'product-sub-category-001', sortOrder: 1 },
    { id: 'product-group-002', groupCode: 'TAB-ANTIBIOTIC', groupName: 'Antibiotics', subCategoryId: 'product-sub-category-001', sortOrder: 2 },
    { id: 'product-group-003', groupCode: 'PPE-GLOVES', groupName: 'Gloves', subCategoryId: 'product-sub-category-003', sortOrder: 1 },
  ],
};
