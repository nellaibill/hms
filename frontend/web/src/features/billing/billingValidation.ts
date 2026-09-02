import { z } from 'zod';
import { isConsultationEntryActive, isLaboratoryEntryActive, isServiceEntryActive, isSimpleServiceEntryActive } from './billingActivity';
import { PAYMENT_METHODS } from './types';

/**
 * Each billing category is entirely optional (per the spec: a receptionist may bill only
 * Consultation, only Lab, all four, or none) — so nothing is `required` at the field level.
 * Instead, `superRefine` treats an entry as "in use" the moment any of its fields has a
 * value, and only then enforces the required-field/discount rules. An untouched entry
 * always passes validation — which also means an empty row can simply be removed rather
 * than needing to be "cleared" first.
 *
 * Consultation/Radiology/Laboratory/Procedure/Injection/File are all arrays — a visit can need
 * several lab tests, several procedures, or (a patient seen by more than one specialist in one
 * visit) several consultations.
 *
 * Payment collection (`payments`) is deliberately *not* a per-line field either, for the same
 * reason — a visit is settled in one transaction at the counter. It's mandatory once anything
 * is billed: the patient is only sent through to the consultant once the counter is paid in
 * full, so the top-level superRefine below rejects a save unless every row has a method and the
 * rows' amounts add up correctly. `payments` is a list rather than a single amount/mode/
 * reference set so the counter can split one bill across more than one method (part Cash, part
 * UPI) — the common case is still exactly one row. No row may ever exceed the net total (capped
 * both here and in BillingSummaryCard's Amount input) — there's no "tender more, get change
 * back" flow from this screen.
 */
export const consultationBillingSchema = z
  .object({
    departmentId: z.string().default(''),
    consultantId: z.string().default(''),
    consultationTypeId: z.string().default(''),
    charge: z.number().min(0).default(0),
    quantity: z.number().int().min(1).default(1),
    discount: z.number().min(0).default(0),
    discountApproved: z.boolean().default(false),
    discountApprovedBy: z.string().default(''),
    // True only for a row InvoiceCreatePage.tsx prefilled from the patient's own recorded
    // visit — ConsultationBillingCard locks Department/Consultant for such a row, so billing
    // can't silently disagree with who the visit record says actually saw the patient.
    // Consultation Type deliberately stays editable even when prefilled: unlike
    // Department/Consultant it isn't always captured at Registration Details (it's optional
    // there), so locking it would sometimes lock in a *blank*, required field with no way to
    // fill it in — and it's the more billing-side judgment call anyway (a visit that ran
    // longer than expected legitimately might warrant a different consultation category),
    // matching the same reasoning already applied to Charge just below. A row added via "Add
    // another Consultation" isn't tied to any visit, so it keeps the default false and stays
    // fully editable.
    fromVisit: z.boolean().default(false),
  })
  .superRefine((data, ctx) => {
    if (!isConsultationEntryActive(data)) return;
    if (!data.departmentId) ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['departmentId'], message: 'Department is required' });
    if (!data.consultantId) ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['consultantId'], message: 'Consultant is required' });
    if (!data.consultationTypeId) {
      ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['consultationTypeId'], message: 'Consultation type is required' });
    }
    if (data.discount > data.charge * data.quantity) {
      ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['discount'], message: 'Discount cannot exceed the charge' });
    }
  });

export const serviceBillingSchema = z
  .object({
    serviceId: z.string().default(''),
    consultantId: z.string().default(''),
    charge: z.number().min(0).default(0),
    quantity: z.number().int().min(1).default(1),
    discount: z.number().min(0).default(0),
    discountApproved: z.boolean().default(false),
    discountApprovedBy: z.string().default(''),
  })
  .superRefine((data, ctx) => {
    if (!isServiceEntryActive(data)) return;
    if (!data.serviceId) ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['serviceId'], message: 'Service is required' });
    if (!data.consultantId) ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['consultantId'], message: 'Consultant is required' });
    if (data.discount > data.charge * data.quantity) {
      ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['discount'], message: 'Discount cannot exceed the charge' });
    }
  });

/**
 * Injection/File rows — same shape as serviceBillingSchema minus consultantId: neither category
 * involves a doctor/consultant, just a priced service picked off the shared DiagnosticTest
 * catalog (Masters' DiagnosticTestServiceType.Injection/File).
 */
export const simpleServiceBillingSchema = z
  .object({
    serviceId: z.string().default(''),
    charge: z.number().min(0).default(0),
    quantity: z.number().int().min(1).default(1),
    discount: z.number().min(0).default(0),
    discountApproved: z.boolean().default(false),
    discountApprovedBy: z.string().default(''),
  })
  .superRefine((data, ctx) => {
    if (!isSimpleServiceEntryActive(data)) return;
    if (!data.serviceId) ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['serviceId'], message: 'Service is required' });
    if (data.discount > data.charge * data.quantity) {
      ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['discount'], message: 'Discount cannot exceed the charge' });
    }
  });

/**
 * Laboratory's own row schema — forked from serviceBillingSchema rather than reusing it,
 * since a Laboratory row can pick either a DiagnosticService or a DiagnosticPackage
 * (itemType/itemId), not just a service (serviceId). Radiology/Procedure are untouched and
 * keep using serviceBillingSchema/serviceArraySchema above. Same superRefine rules as
 * serviceBillingSchema (required-once-active, discount <= charge*quantity), just keyed off
 * itemType+itemId instead of serviceId.
 */
export const laboratoryBillingSchema = z
  .object({
    itemType: z.enum(['service', 'package']).default('service'),
    itemId: z.string().default(''),
    consultantId: z.string().default(''),
    charge: z.number().min(0).default(0),
    quantity: z.number().int().min(1).default(1),
    discount: z.number().min(0).default(0),
    discountApproved: z.boolean().default(false),
    discountApprovedBy: z.string().default(''),
  })
  .superRefine((data, ctx) => {
    if (!isLaboratoryEntryActive(data)) return;
    if (!data.itemId) ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['itemId'], message: 'Item is required' });
    if (!data.consultantId) ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['consultantId'], message: 'Consultant is required' });
    if (data.discount > data.charge * data.quantity) {
      ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['discount'], message: 'Discount cannot exceed the charge' });
    }
  });

function laboratoryArraySchema() {
  return z.array(laboratoryBillingSchema).superRefine((rows, ctx) => {
    const firstIndexByKey = new Map<string, number>();
    rows.forEach((row, index) => {
      if (!row.itemId) return;
      const key = `${row.itemType}:${row.itemId}`;
      if (!firstIndexByKey.has(key)) {
        firstIndexByKey.set(key, index);
        return;
      }
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        path: [index, 'itemId'],
        message: 'This item is already added in another row above — remove it here, or change the row above.',
      });
    });
  });
}

/**
 * Flags a row as a duplicate of an earlier row in the same array — same service picked twice,
 * or (for Consultation) the exact same department+consultant+type combo twice. Only rows with
 * every key field filled in are compared, so a still-being-filled-out row never gets flagged
 * against another still-being-filled-out row. The error lands on the *later* row (the one the
 * receptionist just added), matching how MasterForm's uniqueness check flags the new entry
 * rather than the original.
 */
function serviceArraySchema() {
  return z.array(serviceBillingSchema).superRefine((rows, ctx) => {
    const firstIndexById = new Map<string, number>();
    rows.forEach((row, index) => {
      if (!row.serviceId) return;
      if (!firstIndexById.has(row.serviceId)) {
        firstIndexById.set(row.serviceId, index);
        return;
      }
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        path: [index, 'serviceId'],
        message: 'This service is already added in another row above — remove it here, or change the row above.',
      });
    });
  });
}

/** Injection/File array — same duplicate-service check as serviceArraySchema, keyed off the
 * same serviceId field (these rows just lack consultantId). */
function simpleServiceArraySchema() {
  return z.array(simpleServiceBillingSchema).superRefine((rows, ctx) => {
    const firstIndexById = new Map<string, number>();
    rows.forEach((row, index) => {
      if (!row.serviceId) return;
      if (!firstIndexById.has(row.serviceId)) {
        firstIndexById.set(row.serviceId, index);
        return;
      }
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        path: [index, 'serviceId'],
        message: 'This service is already added in another row above — remove it here, or change the row above.',
      });
    });
  });
}

function consultationArraySchema() {
  return z.array(consultationBillingSchema).superRefine((rows, ctx) => {
    const firstIndexByKey = new Map<string, number>();
    rows.forEach((row, index) => {
      if (!row.departmentId || !row.consultantId || !row.consultationTypeId) return;
      const key = `${row.departmentId}|${row.consultantId}|${row.consultationTypeId}`;
      if (!firstIndexByKey.has(key)) {
        firstIndexByKey.set(key, index);
        return;
      }
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        path: [index, 'consultationTypeId'],
        message: 'This same department, consultant, and consultation type is already added in another row above.',
      });
    });
  });
}

/** Local, self-contained net-total calculation — mirrors billingCalculations.ts's
 * summarizeBilling but kept separate to avoid a circular import (billingCalculations.ts
 * already imports types from this file). Only what the superRefine below needs: gross minus
 * per-line discount, clamped at 0. */
function computeNetTotal(values: {
  consultation: ConsultationBillingFormValues[];
  radiology: ServiceBillingRowFormValues[];
  laboratory: LaboratoryBillingRowFormValues[];
  procedure: ServiceBillingRowFormValues[];
  injection: SimpleServiceBillingRowFormValues[];
  file: SimpleServiceBillingRowFormValues[];
}): number {
  const rows: { charge: number; quantity: number; discount: number }[] = [
    ...values.consultation.filter(isConsultationEntryActive),
    ...values.radiology.filter(isServiceEntryActive),
    ...values.laboratory.filter(isLaboratoryEntryActive),
    ...values.procedure.filter(isServiceEntryActive),
    ...values.injection.filter(isSimpleServiceEntryActive),
    ...values.file.filter(isSimpleServiceEntryActive),
  ];
  const gross = rows.reduce((sum, row) => sum + row.charge * row.quantity, 0);
  const discount = rows.reduce((sum, row) => sum + row.discount, 0);
  return Math.max(gross - discount, 0);
}

/** One payment-method row within `payments` below — see that field's own comment for why this
 * is a list rather than a single amount/mode/reference set. */
export const paymentSplitSchema = z.object({
  method: z.enum(PAYMENT_METHODS).optional(),
  amount: z.number().min(0).default(0),
  referenceNumber: z.string().default(''),
});

export const billingFormSchema = z
  .object({
    consultation: consultationArraySchema().default([]),
    radiology: serviceArraySchema().default([]),
    laboratory: laboratoryArraySchema().default([]),
    procedure: serviceArraySchema().default([]),
    injection: simpleServiceArraySchema().default([]),
    file: simpleServiceArraySchema().default([]),
    payments: z.array(paymentSplitSchema).default([{ method: undefined, amount: 0, referenceNumber: '' }]),
  })
  .superRefine((data, ctx) => {
    const netTotal = computeNetTotal(data);
    // Nothing billed yet — "at least one item is required" is enforced separately once Save
    // is actually attempted (apiBillingRepository.ts), not here on every keystroke.
    if (netTotal <= 0) return;

    // Full payment is required to save from this screen — the patient is only sent through to
    // the consultant once billing is settled at the counter, so there's no "save as Pending
    // and collect later" path here anymore (unlike the standalone Invoice Detail page, which
    // still supports recording payment after the fact for whatever reason). Every row present
    // needs its own method — an abandoned, still-empty "add another" row can't silently ride
    // along as free money from nowhere.
    data.payments.forEach((row, index) => {
      if (!row.method) {
        ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['payments', index, 'method'], message: 'Payment mode is required' });
      }
      // No row may tender more than the whole bill — there's no "give change back" flow from
      // this screen, so an amount above Net Payable can only ever be a mistake (e.g. a stray
      // extra digit), not a legitimate cash-tendered figure.
      if (row.amount > netTotal) {
        ctx.addIssue({
          code: z.ZodIssueCode.custom,
          path: ['payments', index, 'amount'],
          message: 'Amount cannot exceed the Net Payable amount',
        });
      }
    });

    const totalTendered = data.payments.reduce((sum, row) => sum + row.amount, 0);
    if (totalTendered < netTotal) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        path: data.payments.length > 1 ? ['payments'] : ['payments', 0, 'amount'],
        message: 'Full payment is required before this invoice can be saved',
      });
    }
  });

export type BillingFormValues = z.infer<typeof billingFormSchema>;
export type ConsultationBillingFormValues = z.infer<typeof consultationBillingSchema>;
export type ServiceBillingRowFormValues = z.infer<typeof serviceBillingSchema>;
export type LaboratoryBillingRowFormValues = z.infer<typeof laboratoryBillingSchema>;
export type SimpleServiceBillingRowFormValues = z.infer<typeof simpleServiceBillingSchema>;
export type PaymentSplitFormValues = z.infer<typeof paymentSplitSchema>;
/** Radiology/Procedure only now — Laboratory forked off to its own schema/row shape above. */
export type ServiceBillingCategory = 'radiology' | 'procedure';
/** Injection/File — same row shape as each other, minus consultantId (see simpleServiceBillingSchema). */
export type SimpleServiceBillingCategory = 'injection' | 'file';

export const emptyConsultation: ConsultationBillingFormValues = {
  departmentId: '',
  consultantId: '',
  consultationTypeId: '',
  charge: 0,
  quantity: 1,
  discount: 0,
  discountApproved: false,
  discountApprovedBy: '',
  fromVisit: false,
};

export const emptyServiceRow: ServiceBillingRowFormValues = {
  serviceId: '',
  consultantId: '',
  charge: 0,
  quantity: 1,
  discount: 0,
  discountApproved: false,
  discountApprovedBy: '',
};

export const emptyLaboratoryRow: LaboratoryBillingRowFormValues = {
  itemType: 'service',
  itemId: '',
  consultantId: '',
  charge: 0,
  quantity: 1,
  discount: 0,
  discountApproved: false,
  discountApprovedBy: '',
};

export const emptySimpleServiceRow: SimpleServiceBillingRowFormValues = {
  serviceId: '',
  charge: 0,
  quantity: 1,
  discount: 0,
  discountApproved: false,
  discountApprovedBy: '',
};

export const emptyPaymentSplit: PaymentSplitFormValues = {
  method: undefined,
  amount: 0,
  referenceNumber: '',
};

export const defaultBillingFormValues: BillingFormValues = {
  consultation: [{ ...emptyConsultation }],
  radiology: [{ ...emptyServiceRow }],
  laboratory: [{ ...emptyLaboratoryRow }],
  procedure: [{ ...emptyServiceRow }],
  injection: [{ ...emptySimpleServiceRow }],
  file: [{ ...emptySimpleServiceRow }],
  payments: [{ ...emptyPaymentSplit }],
};
