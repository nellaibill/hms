import { useMutation, useQueryClient } from '@tanstack/react-query';
import { getMasterStore } from '../engine/registry';

/**
 * A handful of Masters entities also have their own dedicated picker component
 * (ConsultantSelect, DepartmentSelect, AppointmentTypeSelect, ConsultationTypeSelect) used
 * elsewhere in the app (Registration, Billing, …) — those were built before this generic
 * engine existed and query under their own top-level key rather than `['masters', entityKey,
 * 'options']` the way DesignationSelect does. Without this, saving an edit in Masters (e.g.
 * a Consultant's new Priority) invalidates only the Masters admin screen's own cache; every
 * picker elsewhere keeps serving its stale list until it separately refetches on its own
 * (page remount, cache staleness, etc.) — confirmed live: a Consultant's Priority change
 * wasn't reflected in Billing/Registration's consultant dropdown until a hard refresh.
 * react-query's invalidateQueries matches by prefix, so invalidating the one-element key
 * below covers every picker's differently-suffixed variant (e.g. both `['consultants',
 * 'select-list']` and `['consultants', 'select-list', departmentId]`) in one call.
 */
const PICKER_QUERY_KEY_BY_ENTITY: Partial<Record<string, string>> = {
  consultant: 'consultants',
  department: 'departments',
  appointmentType: 'appointmentTypes',
  consultationType: 'consultationTypes',
};

function useInvalidateMasters(entityKey: string) {
  const queryClient = useQueryClient();
  return () => {
    queryClient.invalidateQueries({ queryKey: ['masters', entityKey] });
    const pickerKey = PICKER_QUERY_KEY_BY_ENTITY[entityKey];
    if (pickerKey) {
      queryClient.invalidateQueries({ queryKey: [pickerKey] });
    }
  };
}

export function useCreateMasterMutation(entityKey: string) {
  const invalidate = useInvalidateMasters(entityKey);
  return useMutation({
    mutationFn: async (values: Record<string, unknown>) => {
      const store = getMasterStore(entityKey);
      if (!store) throw new Error(`Unknown Masters entity "${entityKey}".`);
      return store.create(values);
    },
    onSuccess: invalidate,
  });
}

export function useUpdateMasterMutation(entityKey: string) {
  const invalidate = useInvalidateMasters(entityKey);
  return useMutation({
    mutationFn: async ({ id, values }: { id: string; values: Record<string, unknown> }) => {
      const store = getMasterStore(entityKey);
      if (!store) throw new Error(`Unknown Masters entity "${entityKey}".`);
      return store.update(id, values);
    },
    onSuccess: invalidate,
  });
}
