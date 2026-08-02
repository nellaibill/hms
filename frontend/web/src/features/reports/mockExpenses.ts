import type { ExpenseReportRow } from './types';

/**
 * Seed hospital expense entries for the Income & Expense Report — there's no Expense
 * module/data-entry flow anywhere in the app yet (unlike Billing, which has a real, if
 * mock-backed, entry point), so this is report-display data only, modeled on the same
 * Tirunelveli hospital as features/patients/mockPatients.ts. Spans the same date window as
 * the billing seed data so the report reads as one coherent month, not two disjoint ranges.
 */
export const MOCK_EXPENSES: ExpenseReportRow[] = [
  { id: 'mock-exp-001', date: '2026-07-21', category: 'Staff Salaries', description: 'Nursing staff — July payroll (partial)', amount: 185000 },
  { id: 'mock-exp-002', date: '2026-07-22', category: 'Pharmacy Purchase', description: 'Antibiotics & IV fluids restock', amount: 42500 },
  { id: 'mock-exp-003', date: '2026-07-23', category: 'Utilities', description: 'Electricity board — monthly bill', amount: 68000 },
  { id: 'mock-exp-004', date: '2026-07-24', category: 'Equipment Maintenance', description: 'CT scanner annual service contract', amount: 35000 },
  { id: 'mock-exp-005', date: '2026-07-25', category: 'Housekeeping & Supplies', description: 'Cleaning supplies and linen', amount: 12500 },
  { id: 'mock-exp-006', date: '2026-07-27', category: 'Insurance Premium', description: 'Hospital indemnity insurance — quarterly', amount: 95000 },
  { id: 'mock-exp-007', date: '2026-07-28', category: 'Ambulance', description: 'Fuel and vehicle upkeep', amount: 8200 },
  { id: 'mock-exp-008', date: '2026-07-29', category: 'Lab Consumables', description: 'Reagents and test kits', amount: 27800 },
  { id: 'mock-exp-009', date: '2026-07-30', category: 'Staff Salaries', description: 'Consultant fees — visiting specialists', amount: 120000 },
  { id: 'mock-exp-010', date: '2026-08-01', category: 'Miscellaneous', description: 'Office supplies and printing', amount: 4300 },
];
