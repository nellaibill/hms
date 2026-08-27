import { CalendarClock } from 'lucide-react';
import type { MasterEntityConfig } from '../engine/types';

export const appointmentTypeConfig: MasterEntityConfig = {
  key: 'appointmentType',
  label: 'Appointment Type',
  labelPlural: 'Appointment Types',
  description: 'OP appointment categories (e.g. New, Follow-up, Referral) — shared reference data for Patient registration.',
  icon: CalendarClock,
  section: 'Hospital Reference Data',
  nameField: 'name',
  fields: [
    { key: 'name', label: 'Appointment Type Name', type: 'text', required: true },
  ],
};
