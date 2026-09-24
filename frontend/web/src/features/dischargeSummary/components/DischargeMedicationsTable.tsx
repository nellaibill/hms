import { FOOD_INSTRUCTIONS, type DischargeSummaryFormValues } from '@hms/shared';
import { Plus, Trash2 } from 'lucide-react';
import { Controller, useFieldArray, type Control, type FieldErrors, type UseFormRegister } from 'react-hook-form';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';

const foodInstructionLabels: Record<(typeof FOOD_INSTRUCTIONS)[number], string> = {
  BeforeFood: 'Before food',
  AfterFood: 'After food',
};

interface DischargeMedicationsTableProps {
  control: Control<DischargeSummaryFormValues>;
  register: UseFormRegister<DischargeSummaryFormValues>;
  errors: FieldErrors<DischargeSummaryFormValues>;
}

/**
 * Dynamic add/remove row table for the Discharge Medications section — columns match the
 * approved plan's PDF layout (drug/dose/route/M/N/E/N qty/days/food-instruction). Discharge
 * medications can't be sourced from Pharmacy (no Prescription entity with dose/route/
 * frequency), so this is direct entry, replaced wholesale on every save (list-sync, no
 * per-line CRUD — mirrors the backend's UpdateDischargeSummaryRequest.Medications).
 */
export function DischargeMedicationsTable({ control, register, errors }: DischargeMedicationsTableProps) {
  const { fields, append, remove } = useFieldArray({ control, name: 'medications' });

  function addRow() {
    append({
      sortOrder: fields.length,
      drugName: '',
      dose: '',
      route: '',
      morningQty: 0,
      noonQty: 0,
      eveningQty: 0,
      nightQty: 0,
      durationDays: 0,
      foodInstruction: 'AfterFood',
    });
  }

  return (
    <div className="flex flex-col gap-3">
      <div className="overflow-x-auto rounded-md border border-border">
        <table className="w-full min-w-[880px] text-sm">
          <thead className="bg-sidebar-active text-xs uppercase text-sidebar-active-foreground">
            <tr>
              <th className="px-2 py-2 text-left">Drug</th>
              <th className="px-2 py-2 text-left">Dose</th>
              <th className="px-2 py-2 text-left">Route</th>
              <th className="w-14 px-2 py-2 text-left">M</th>
              <th className="w-14 px-2 py-2 text-left">N</th>
              <th className="w-14 px-2 py-2 text-left">E</th>
              <th className="w-14 px-2 py-2 text-left">N</th>
              <th className="w-16 px-2 py-2 text-left">Days</th>
              <th className="px-2 py-2 text-left">Food</th>
              <th className="w-10 px-2 py-2" />
            </tr>
          </thead>
          <tbody>
            {fields.length === 0 && (
              <tr>
                <td colSpan={10} className="px-2 py-4 text-center text-muted-foreground">
                  No medications added yet.
                </td>
              </tr>
            )}
            {fields.map((field, index) => (
              <tr key={field.id} className="border-t border-border align-top">
                <td className="px-2 py-1.5">
                  <Input aria-label="Drug name" {...register(`medications.${index}.drugName`)} />
                  {errors.medications?.[index]?.drugName && (
                    <p className="mt-1 text-xs text-destructive">{errors.medications[index]?.drugName?.message}</p>
                  )}
                </td>
                <td className="px-2 py-1.5">
                  <Input aria-label="Dose" {...register(`medications.${index}.dose`)} />
                  {errors.medications?.[index]?.dose && (
                    <p className="mt-1 text-xs text-destructive">{errors.medications[index]?.dose?.message}</p>
                  )}
                </td>
                <td className="px-2 py-1.5">
                  <Input aria-label="Route" {...register(`medications.${index}.route`)} />
                  {errors.medications?.[index]?.route && (
                    <p className="mt-1 text-xs text-destructive">{errors.medications[index]?.route?.message}</p>
                  )}
                </td>
                <td className="px-2 py-1.5">
                  <Input aria-label="Morning quantity" type="number" step="0.5" min={0} {...register(`medications.${index}.morningQty`)} />
                </td>
                <td className="px-2 py-1.5">
                  <Input aria-label="Noon quantity" type="number" step="0.5" min={0} {...register(`medications.${index}.noonQty`)} />
                </td>
                <td className="px-2 py-1.5">
                  <Input aria-label="Evening quantity" type="number" step="0.5" min={0} {...register(`medications.${index}.eveningQty`)} />
                </td>
                <td className="px-2 py-1.5">
                  <Input aria-label="Night quantity" type="number" step="0.5" min={0} {...register(`medications.${index}.nightQty`)} />
                </td>
                <td className="px-2 py-1.5">
                  <Input aria-label="Duration in days" type="number" min={0} {...register(`medications.${index}.durationDays`)} />
                </td>
                <td className="px-2 py-1.5">
                  <Controller
                    control={control}
                    name={`medications.${index}.foodInstruction`}
                    render={({ field: foodField }) => (
                      <Select value={foodField.value} onValueChange={foodField.onChange}>
                        <SelectTrigger aria-label="Food instruction">
                          <SelectValue />
                        </SelectTrigger>
                        <SelectContent>
                          {FOOD_INSTRUCTIONS.map((instruction) => (
                            <SelectItem key={instruction} value={instruction}>
                              {foodInstructionLabels[instruction]}
                            </SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                    )}
                  />
                </td>
                <td className="px-2 py-1.5">
                  <Button type="button" variant="ghost" size="icon" aria-label="Remove medication" onClick={() => remove(index)}>
                    <Trash2 className="h-4 w-4 text-destructive" />
                  </Button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <Button type="button" variant="outline" size="sm" className="w-fit gap-1.5" onClick={addRow}>
        <Plus className="h-4 w-4" />
        Add Medication
      </Button>
    </div>
  );
}
