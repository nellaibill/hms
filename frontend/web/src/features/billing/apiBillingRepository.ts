import type {
  CreateInvoiceRequest as CreateInvoiceRequestDto,
  InvoiceListQuery as InvoiceListQueryDto,
  InvoiceResponse as InvoiceResponseDto,
  PaginationMeta,
} from '@hms/shared';
import { NetworkError } from '@hms/shared';
import { billingApi } from '@/services/apiClient';
import {
  getAllMockBillings,
  getBillingForPatient,
  getMockBillingById,
  listMockBillings,
  recordMockPayment,
  saveBillingForPatient,
  type PatientDisplaySnapshot,
} from './mockBillingStore';
import type { BillingFormValues } from './billingValidation';
import { toBillingItems } from './billingCalculations';
import type { Billing, BillingItem, PaymentStatus } from './types';

/**
 * Real, database-backed repository (HMS.Modules.Billing) — implements the same shape as
 * mockBillingStore.ts so the hooks built against the mock store keep working unchanged.
 * Falls back to that same mock store on NetworkError (backend genuinely unreachable, e.g.
 * the static GitHub Pages deploy) — a real ApiError (backend up, request rejected) still
 * surfaces normally. Mirrors the pattern in features/roles/apiRoleRepository.ts and
 * features/patients/mockPatientsStore.ts.
 */

export interface PagedBillings {
  items: Billing[];
  meta: PaginationMeta;
  /** 'mock' when the real backend was unreachable and this list came from the offline fallback below. */
  source: 'live' | 'mock';
}

function fromItemDto(item: InvoiceResponseDto['items'][number]): BillingItem {
  return {
    id: item.id,
    billingType: item.billingType,
    departmentId: item.departmentId ?? undefined,
    consultantId: item.consultantId ?? undefined,
    serviceId: item.serviceId ?? undefined,
    quantity: item.quantity,
    unitPrice: item.unitPrice,
    discount: item.discount,
    discountApproved: item.discountApproved,
    discountApprovedBy: item.discountApprovedBy ?? undefined,
    paymentStatus: item.paymentStatus,
    total: item.total,
  };
}

function fromDto(dto: InvoiceResponseDto): Billing {
  return {
    id: dto.id,
    invoiceNumber: dto.invoiceNumber,
    patientId: dto.patientId,
    visitId: dto.visitId,
    patientName: dto.patientName,
    patientUhid: dto.patientUhid,
    createdAt: dto.createdAt,
    items: dto.items.map(fromItemDto),
    grossAmount: dto.grossAmount,
    totalDiscount: dto.totalDiscount,
    netAmount: dto.netAmount,
  };
}

function toCreateRequest(patientId: string, visitId: string, values: BillingFormValues, patient: PatientDisplaySnapshot): CreateInvoiceRequestDto {
  return {
    patientId,
    visitId,
    patientName: patient.name,
    patientUhid: patient.uhid,
    items: toBillingItems(values).map((item) => ({
      billingType: item.billingType,
      departmentId: item.departmentId ?? null,
      consultantId: item.consultantId ?? null,
      serviceId: item.serviceId ?? null,
      quantity: item.quantity,
      unitPrice: item.unitPrice,
      discount: item.discount,
      discountApproved: item.discountApproved,
      discountApprovedBy: item.discountApprovedBy ?? null,
    })),
  };
}

function toListQueryDto(query: { page?: number; pageSize?: number; sort?: string; search?: string; paymentStatus?: PaymentStatus }): InvoiceListQueryDto {
  return {
    page: query.page,
    pageSize: query.pageSize,
    sort: query.sort,
    search: query.search,
    paymentStatus: query.paymentStatus,
  };
}

export async function listInvoices(query: {
  page?: number;
  pageSize?: number;
  sort?: string;
  search?: string;
  paymentStatus?: PaymentStatus;
}): Promise<PagedBillings> {
  try {
    const paged = await billingApi.getInvoices(toListQueryDto(query));
    return { items: paged.items.map(fromDto), meta: paged.meta, source: 'live' };
  } catch (err) {
    if (err instanceof NetworkError) {
      return { ...listMockBillings(query), source: 'mock' };
    }
    throw err;
  }
}

export async function getInvoiceById(id: string): Promise<Billing> {
  try {
    const dto = await billingApi.getInvoiceById(id);
    return fromDto(dto);
  } catch (err) {
    if (err instanceof NetworkError) {
      const mock = getMockBillingById(id);
      if (mock) return mock;
    }
    throw err;
  }
}

/** Returns null when every billing category was left blank — an empty bill isn't a record worth keeping (matches mockBillingStore.ts's saveBillingForPatient, and CreateInvoiceRequestValidator's server-side rejection of the same case). */
export async function createInvoice(
  patientId: string,
  visitId: string,
  values: BillingFormValues,
  patient: PatientDisplaySnapshot,
): Promise<Billing | null> {
  if (toBillingItems(values).length === 0) return null;

  try {
    const created = await billingApi.createInvoice(toCreateRequest(patientId, visitId, values, patient));
    return fromDto(created);
  } catch (err) {
    if (err instanceof NetworkError) {
      return saveBillingForPatient(patientId, visitId, values, patient);
    }
    throw err;
  }
}

export async function recordPayment(billingId: string, itemId: string, method: 'Cash' | 'Card' | 'Upi' | 'BankTransfer'): Promise<Billing> {
  try {
    const updated = await billingApi.recordPayment(billingId, itemId, { method });
    return fromDto(updated);
  } catch (err) {
    if (err instanceof NetworkError) {
      return recordMockPayment(billingId, itemId);
    }
    throw err;
  }
}

export async function getInvoicesByPatientId(patientId: string): Promise<Billing[]> {
  try {
    const invoices = await billingApi.getInvoicesByPatientId(patientId);
    return invoices.map(fromDto);
  } catch (err) {
    if (err instanceof NetworkError) {
      return getBillingForPatient(patientId);
    }
    throw err;
  }
}

/** Unpaginated, unfiltered — for report aggregation (features/reports). Real invoices are
 * fetched at the server's maximum page size (PagedRequest.MaxPageSize = 100); a hospital
 * issuing more than 100 invoices in a report's date range will only see the first page's
 * worth here until this gets a dedicated report endpoint — a known limitation, not silently
 * wrong data (the ledger itself is properly paginated). */
export async function getAllInvoicesForReport(): Promise<Billing[]> {
  try {
    const paged = await billingApi.getInvoices({ page: 1, pageSize: 100 });
    return paged.items.map(fromDto);
  } catch (err) {
    if (err instanceof NetworkError) {
      return getAllMockBillings();
    }
    throw err;
  }
}
