import type { StaffDirectoryEntry } from '@hms/shared';
import { SearchableSelect, type SearchableSelectOption } from '@/components/ui/searchable-select';

interface StaffPickerProps {
  id: string;
  ariaLabel: string;
  value: string;
  onValueChange: (value: string) => void;
  staff: StaffDirectoryEntry[];
  placeholder?: string;
}

/** One optional staff picker for the Finalization sign-off section (Prepared/Checked/
 * Consultant Approved By), backed by the shared staff directory. */
export function StaffPicker({ id, ariaLabel, value, onValueChange, staff, placeholder = 'Select staff member…' }: StaffPickerProps) {
  // A leading "— None —" option lets an already-picked, optional sign-off field be cleared
  // again — every field here (Prepared/Checked/Consultant Approved By) is optional per the
  // backend contract, so the user must be able to deselect, not just switch between names.
  const options: SearchableSelectOption[] = [
    { value: '', label: '— None —' },
    ...staff.map((entry) => ({
      value: entry.id,
      label: `${entry.firstName} ${entry.lastName}`,
      keywords: entry.roleName,
    })),
  ];

  return (
    <SearchableSelect
      id={id}
      ariaLabel={ariaLabel}
      value={value}
      onValueChange={onValueChange}
      options={options}
      placeholder={placeholder}
      searchPlaceholder="Search staff…"
    />
  );
}
