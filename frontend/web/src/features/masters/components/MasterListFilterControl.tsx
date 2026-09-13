import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { useMasterOptionsQuery } from '../hooks/useMasterQuery';
import { getMasterConfig, getDisplayLabel } from '../engine/registry';
import type { MasterFieldDef } from '../engine/types';

interface MasterListFilterControlProps {
  field: MasterFieldDef;
  value: string | undefined;
  onChange: (value: string | undefined) => void;
}

const ALL_VALUE = '__all__';

/** One extra filter dropdown on a Masters list page's toolbar (see MasterEntityConfig.listFilters)
 * — populated from the same field definition a create/edit form field for this key would use:
 * a 'reference' field fetches that entity's records, 'select'/'radio-card' uses its own options. */
export function MasterListFilterControl({ field, value, onChange }: MasterListFilterControlProps) {
  const referenceOptions = useMasterOptionsQuery(field.type === 'reference' ? field.referenceEntityKey : undefined);
  const referenceConfig = field.type === 'reference' ? getMasterConfig(field.referenceEntityKey) : undefined;

  const items =
    field.type === 'reference'
      ? (referenceOptions.data ?? []).map((option) => ({
          value: option.id,
          label: referenceConfig ? getDisplayLabel(referenceConfig, option) : option.id,
        }))
      : (field.options ?? []).map((option) => ({ value: option.value, label: option.label }));

  return (
    <Select value={value ?? ALL_VALUE} onValueChange={(next) => onChange(next === ALL_VALUE ? undefined : next)}>
      <SelectTrigger className="w-48" aria-label={`Filter by ${field.label}`}>
        <SelectValue placeholder={`All ${field.label.toLowerCase()}s`} />
      </SelectTrigger>
      <SelectContent>
        <SelectItem value={ALL_VALUE}>All {field.label.toLowerCase()}s</SelectItem>
        {items.map((item) => (
          <SelectItem key={item.value} value={item.value}>
            {item.label}
          </SelectItem>
        ))}
      </SelectContent>
    </Select>
  );
}
