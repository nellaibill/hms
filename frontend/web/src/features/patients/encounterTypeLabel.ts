import type { EncounterTypeUi } from '@hms/shared';

/**
 * Duration guidance appended to each encounter type so reception picks the right one — OP
 * has no duration guidance to add and stays plain. Label only: the underlying value is
 * unchanged.
 */
const LABELS: Record<EncounterTypeUi, string> = {
  OP: 'OP',
  IP: 'IP (Inpatient): More than 12 hours',
  Emergency: 'Emergency: Emergency cases',
  DayCare: 'Day Care: 6–12 hours',
  Observation: 'Observation: 1–6 hours',
};

export function encounterTypeLabel(encounterType: EncounterTypeUi): string {
  return LABELS[encounterType];
}

/**
 * Plain name only, no duration guidance — for the Select trigger's closed-state display
 * (and anywhere else the choice is shown back after selection), so it reads "Day Care" or
 * "IP" rather than the full dropdown-list description. "DayCare" is the one raw value that
 * needs a space added; every other value already reads fine as-is.
 */
export function encounterTypeShortLabel(encounterType: EncounterTypeUi): string {
  return encounterType === 'DayCare' ? 'Day Care' : encounterType;
}
