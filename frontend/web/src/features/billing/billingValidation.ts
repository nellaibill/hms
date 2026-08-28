import { z } from 'zod';
import { isConsultationEntryActive, isServiceEntryActive } from './billingActivity';
import { PAYMENT_STATUSES } from './types';

/**
 * Each billing category is entirely optional (per the spec: a receptionist may bill only
 * Consultation, only Lab, all four, or none) — so nothing is `required` at the field level.
 * Instead, `superRefine` treats an entry as "in use" the moment any of its fields has a
 * value, and only then enforces the required-field/discount rules. An untouched entry
 * always passes validation — which also means an empty row can simply be removed rather
 * than needing to be "cleared" first.
 *
 * Consultation/Radiology/Laboratory/Procedure are all arrays — a visit can need several lab
 * tests, several procedures, or (a patient seen by more than one specialist in one visit)
 * several consultations.
 *
 * Payment status is deliberately *not* a per-line field — a visit is settled in one
 * transaction at the counter, not per category, so asking Pending/Paid four separate times
 * read as confusing/redundant. It's a single top-level field on billingFormSchema instead
 * (see the Billing Summary card), applied to every line item the bill produces.
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

export const billingFormSchema = z.object({
  consultation: consultationArraySchema().default([]),
  radiology: serviceArraySchema().default([]),
  laboratory: serviceArraySchema().default([]),
  procedure: serviceArraySchema().default([]),
  paymentStatus: z.enum(PAYMENT_STATUSES).default('Pending'),
});

export type BillingFormValues = z.infer<typeof billingFormSchema>;
export type ConsultationBillingFormValues = z.infer<typeof consultationBillingSchema>;
export type ServiceBillingRowFormValues = z.infer<typeof serviceBillingSchema>;
export type ServiceBillingCategory = 'radiology' | 'laboratory' | 'procedure';

export const emptyConsultation: ConsultationBillingFormValues = {
  departmentId: '',
  consultantId: '',
  consultationTypeId: '',
  charge: 0,
  quantity: 1,
  discount: 0,
  discountApproved: false,
  discountApprovedBy: '',
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

export const defaultBillingFormValues: BillingFormValues = {
  consultation: [{ ...emptyConsultation }],
  radiology: [{ ...emptyServiceRow }],
  laboratory: [{ ...emptyServiceRow }],
  procedure: [{ ...emptyServiceRow }],
  paymentStatus: 'Pending',
};
