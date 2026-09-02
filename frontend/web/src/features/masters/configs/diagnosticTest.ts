import { FlaskConical } from 'lucide-react';
import type { MasterEntityConfig } from '../engine/types';

export const DIAGNOSTIC_TEST_SERVICE_TYPE_OPTIONS = [
  { value: 'Laboratory', label: 'Laboratory' },
  { value: 'Radiology', label: 'Radiology' },
  { value: 'Procedure', label: 'Procedure' },
  { value: 'Injection', label: 'Injection' },
  { value: 'File', label: 'File' },
];

export const diagnosticTestConfig: MasterEntityConfig = {
  key: 'diagnosticTest',
  label: 'Diagnostic Test',
  labelPlural: 'Diagnostic Tests',
  description: 'Billable laboratory/radiology tests, packages, procedures, injections, and files with their standard price — feeds Billing’s Laboratory, Radiology, Procedure, Injection, and File sections.',
  icon: FlaskConical,
  section: 'Hospital Reference Data',
  nameField: 'name',
  fields: [
    // skipUniquenessCheck: the same test name can legitimately appear twice (e.g. priced
    // in-house and again for an outsourced/reference-lab variant) — the backend's real
    // uniqueness scope is (name, serviceType, isOutsourced), not name alone.
    { key: 'name', label: 'Test Name', type: 'text', required: true, skipUniquenessCheck: true },
    { key: 'serviceType', label: 'Service Type', type: 'select', options: DIAGNOSTIC_TEST_SERVICE_TYPE_OPTIONS, required: true, defaultValue: 'Laboratory' },
    { key: 'category', label: 'Category', type: 'text', placeholder: 'e.g. Haematology, Cardiology, Package', helpText: 'Informational grouping only (Haematology, Biochemistry, Package, etc.).' },
    { key: 'price', label: 'Price (₹)', type: 'decimal', required: true, min: 0, step: 1 },
    { key: 'isOutsourced', label: 'Outsourced to Reference Lab', type: 'boolean', defaultValue: false },
    { key: 'referenceLab', label: 'Reference Lab', type: 'text', placeholder: 'e.g. Q-LAB, ANDERSON', helpText: 'Only relevant when outsourced.' },
  ],
};
