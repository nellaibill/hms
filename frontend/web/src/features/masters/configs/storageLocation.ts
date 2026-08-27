import { MapPin } from 'lucide-react';
import type { MasterEntityConfig } from '../engine/types';

export const LOCATION_TYPE_OPTIONS = [
  { value: 'Rack', label: 'Rack' },
  { value: 'Shelf', label: 'Shelf' },
  { value: 'Bin', label: 'Bin' },
];

export const storageLocationConfig: MasterEntityConfig = {
  key: 'storageLocation',
  label: 'Storage Location',
  labelPlural: 'Storage Locations',
  description: 'Rack/Shelf/Bin hierarchy within a Warehouse.',
  icon: MapPin,
  section: 'Warehouse & Location',
  codeField: 'locationCode',
  nameField: 'locationName',
  uniqueScopeField: 'warehouseId',
  fields: [
    { key: 'warehouseId', label: 'Warehouse', type: 'reference', referenceEntityKey: 'warehouse', required: true },
    { key: 'locationCode', label: 'Location Code', type: 'text', required: true, helpText: 'Must be unique within the selected warehouse.' },
    { key: 'locationName', label: 'Location Name', type: 'text', required: true },
    { key: 'locationType', label: 'Location Type', type: 'select', options: LOCATION_TYPE_OPTIONS, defaultValue: 'Rack' },
    {
      key: 'parentLocationId',
      label: 'Parent Location',
      type: 'reference',
      referenceEntityKey: 'storageLocation',
      referenceScopeField: 'warehouseId',
      excludeSelf: true,
      helpText: 'Optional — e.g. pick a Rack as the parent of a Shelf, in the same warehouse.',
    },
  ],
};
