import { Ruler } from 'lucide-react';
import type { MasterEntityConfig } from '../engine/types';

export const UOM_TYPE_OPTIONS = [
  { value: 'Count', label: 'Count' },
  { value: 'Weight', label: 'Weight' },
  { value: 'Volume', label: 'Volume' },
  { value: 'Length', label: 'Length' },
  { value: 'Area', label: 'Area' },
];

export const unitOfMeasureConfig: MasterEntityConfig = {
  key: 'unitOfMeasure',
  label: 'Unit of Measure',
  labelPlural: 'Units of Measure',
  description: 'Base and derived units used for stock quantities (e.g. Box, Strip, Piece).',
  icon: Ruler,
  section: 'Pharmacy & Inventory',
  codeField: 'uomCode',
  nameField: 'uomName',
  fields: [
    { key: 'uomCode', label: 'UOM Code', type: 'text', required: true },
    { key: 'uomName', label: 'UOM Name', type: 'text', required: true },
    { key: 'uomType', label: 'UOM Type', type: 'select', options: UOM_TYPE_OPTIONS, defaultValue: 'Count' },
    { key: 'isBaseUnit', label: 'Base Unit', type: 'boolean', defaultValue: false, helpText: 'Only one base unit should be allowed per product (enforced in the Product module).' },
  ],
};
