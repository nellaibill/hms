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
  section: 'Units & Tax',
  codeField: 'uomCode',
  nameField: 'uomName',
  fields: [
    { key: 'uomCode', label: 'UOM Code', type: 'text', required: true },
    { key: 'uomName', label: 'UOM Name', type: 'text', required: true },
    { key: 'uomType', label: 'UOM Type', type: 'select', options: UOM_TYPE_OPTIONS, defaultValue: 'Count' },
    { key: 'isBaseUnit', label: 'Base Unit', type: 'boolean', defaultValue: false, helpText: 'Only one base unit should be allowed per product (enforced in the Product module).' },
  ],
  seed: [
    { id: 'uom-001', uomCode: 'PCS', uomName: 'Piece', uomType: 'Count', isBaseUnit: true },
    { id: 'uom-002', uomCode: 'STRIP', uomName: 'Strip', uomType: 'Count', isBaseUnit: false },
    { id: 'uom-003', uomCode: 'BOX', uomName: 'Box', uomType: 'Count', isBaseUnit: false },
    { id: 'uom-004', uomCode: 'KG', uomName: 'Kilogram', uomType: 'Weight', isBaseUnit: true },
    { id: 'uom-005', uomCode: 'GM', uomName: 'Gram', uomType: 'Weight', isBaseUnit: false },
    { id: 'uom-006', uomCode: 'LTR', uomName: 'Litre', uomType: 'Volume', isBaseUnit: true },
    { id: 'uom-007', uomCode: 'ML', uomName: 'Millilitre', uomType: 'Volume', isBaseUnit: false },
  ],
};
