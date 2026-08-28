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
    if (data.discount > data.charge) {
      ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['discount'], message: 'Discount cannot exceed the charge' });
    }
  });

export const serviceBillingSchema = z
  .object({
    serviceId: z.string().default(''),
    consultantId: z.string().default(''),
    charge: z.number().min(0).default(0),
    discount: z.number().min(0).default(0),
    discountApproved: z.boolean().default(false),
    discountApprovedBy: z.string().default(''),
  })
  .superRefine((data, ctx) => {
    if (!isServiceEntryActive(data)) return;
    if (!data.serviceId) ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['serviceId'], message: 'Service is required' });
    if (!data.consultantId) ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['consultantId'], message: 'Consultant is required' });
    if (data.discount > data.charge) {
      ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['discount'], message: 'Discount cannot exceed the charge' });
    }
  });

export const billingFormSchema = z.object({
  consultation: z.array(consultationBillingSchema).default([]),
  radiology: z.array(serviceBillingSchema).default([]),
  laboratory: z.array(serviceBillingSchema).default([]),
  procedure: z.array(serviceBillingSchema).default([]),
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
  discount: 0,
  discountApproved: false,
  discountApprovedBy: '',
};

export const emptyServiceRow: ServiceBillingRowFormValues = {
  serviceId: '',
  consultantId: '',
  charge: 0,
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
