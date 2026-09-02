/**
 * Whether a billing category has any data entered at all — shared by validation (a
 * category with nothing filled in is skipped, since every category is optional) and by the
 * summary/collapsed-card display (only show/total categories the user actually used).
 */

interface ConsultationEntryLike {
  departmentId: string;
  consultantId: string;
  consultationTypeId: string;
  discount: number;
}

interface ServiceEntryLike {
  serviceId: string;
  consultantId: string;
  discount: number;
}

/** Injection/File rows have no consultant field at all (see billingValidation.ts's
 * simpleServiceBillingSchema) — otherwise identical to ServiceEntryLike. */
interface SimpleServiceEntryLike {
  serviceId: string;
  discount: number;
}

/** Laboratory's own row shape (itemType/itemId, not serviceId — see billingValidation.ts's
 * laboratoryBillingSchema) is otherwise identical to ServiceEntryLike. */
interface LaboratoryEntryLike {
  itemId: string;
  consultantId: string;
  discount: number;
}

export function isConsultationEntryActive(entry: ConsultationEntryLike): boolean {
  return Boolean(entry.departmentId || entry.consultantId || entry.consultationTypeId || entry.discount > 0);
}

export function isServiceEntryActive(entry: ServiceEntryLike): boolean {
  return Boolean(entry.serviceId || entry.consultantId || entry.discount > 0);
}

export function isSimpleServiceEntryActive(entry: SimpleServiceEntryLike): boolean {
  return Boolean(entry.serviceId || entry.discount > 0);
}

export function isLaboratoryEntryActive(entry: LaboratoryEntryLike): boolean {
  return Boolean(entry.itemId || entry.consultantId || entry.discount > 0);
}
