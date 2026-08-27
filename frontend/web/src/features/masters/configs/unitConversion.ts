import { ArrowLeftRight } from 'lucide-react';
import type { MasterEntityConfig } from '../engine/types';

export const unitConversionConfig: MasterEntityConfig = {
  key: 'unitConversion',
  label: 'Unit Conversion',
  labelPlural: 'Unit Conversions',
  description: 'Conversion factors between two Units of Measure (e.g. 1 Box = 10 Strip).',
  icon: ArrowLeftRight,
  section: 'Units & Tax',
  fields: [
    { key: 'fromUomId', label: 'From Unit', type: 'reference', referenceEntityKey: 'unitOfMeasure', required: true },
    { key: 'toUomId', label: 'To Unit', type: 'reference', referenceEntityKey: 'unitOfMeasure', required: true },
    { key: 'conversionFactor', label: 'Conversion Factor', type: 'decimal', required: true, min: 0, step: 0.000001, helpText: '1 "From Unit" = this many "To Unit". From and To cannot be the same unit.' },
  ],
  getDisplayLabel: (record, resolveReference) =>
    `${resolveReference('unitOfMeasure', record.fromUomId as string)} → ${resolveReference('unitOfMeasure', record.toUomId as string)}`,
  validateForm: (values) =>
    values.fromUomId && values.toUomId && values.fromUomId === values.toUomId
      ? 'From Unit and To Unit cannot be the same.'
      : undefined,
};
