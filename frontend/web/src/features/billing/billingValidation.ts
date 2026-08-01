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
 * Radiology/Laboratory/Procedure are arrays (a visit can need several lab tests or several
 * procedures); Consultation stays a single entry — one primary consultation per
 * registration is the realistic case, and multi-consultation is a separate future step.
 */
const paymentStatusFieldSchema = z.enum(['', PAYMENT_STATUSES[0], PAYMENT_STATUSES[1]]).default('');

export const consultationBillingSchema = z
  .object({
    departmentId: z.string().default(''),
    consultantId: z.string().default(''),
    consultationTypeId: z.string().default(''),
    charge: z.number().min(0).default(0),
    discount: z.number().min(0).default(0),
    discountApproved: z.boolean().default(false),
    discountApprovedBy: z.string().default(''),
    paymentStatus: paymentStatusFieldSchema,
  })
  .superRefine((data, ctx) => {
    if (!isConsultationEntryActive(data)) return;
    if (!data.departmentId) ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['departmentId'], message: 'Department is required' });
    if (!data.consultantId) ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['consultantId'], message: 'Consultant is required' });
    if (!data.consultationTypeId) {
      ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['consultationTypeId'], message: 'Consultation type is required' });
    }
    if (!data.paymentStatus) ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['paymentStatus'], message: 'Payment status is required' });
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
    paymentStatus: paymentStatusFieldSchema,
  })
  .superRefine((data, ctx) => {
    if (!isServiceEntryActive(data)) return;
    if (!data.serviceId) ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['serviceId'], message: 'Service is required' });
    if (!data.consultantId) ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['consultantId'], message: 'Consultant is required' });
    if (!data.paymentStatus) ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['paymentStatus'], message: 'Payment status is required' });
    if (data.discount > data.charge) {
      ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['discount'], message: 'Discount cannot exceed the charge' });
    }
  });

export const billingFormSchema = z.object({
  consultation: consultationBillingSchema,
  radiology: z.array(serviceBillingSchema).default([]),
  laboratory: z.array(serviceBillingSchema).default([]),
  procedure: z.array(serviceBillingSchema).default([]),
});

export type BillingFormValues = z.infer<typeof billingFormSchema>;
export type ConsultationBillingFormValues = BillingFormValues['consultation'];
export type ServiceBillingRowFormValues = z.infer<typeof serviceBillingSchema>;
export type ServiceBillingCategory = 'radiology' | 'laboratory' | 'procedure';

const emptyConsultation: ConsultationBillingFormValues = {
  departmentId: '',
  consultantId: '',
  consultationTypeId: '',
  charge: 0,
  discount: 0,
  discountApproved: false,
  discountApprovedBy: '',
  paymentStatus: '',
};

export const emptyServiceRow: ServiceBillingRowFormValues = {
  serviceId: '',
  consultantId: '',
  charge: 0,
  discount: 0,
  discountApproved: false,
  discountApprovedBy: '',
  paymentStatus: '',
};

export const defaultBillingFormValues: BillingFormValues = {
  consultation: { ...emptyConsultation },
  radiology: [{ ...emptyServiceRow }],
  laboratory: [{ ...emptyServiceRow }],
  procedure: [{ ...emptyServiceRow }],
};
