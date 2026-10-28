import { API_ROUTES } from '../../constants';
import type {
  ApproveInvoiceRevertRequest,
  BillingDashboardSummary,
  CreateInvoiceRequest,
  CreateInvoiceRevertRequest,
  InvoiceListQuery,
  InvoiceResponse,
  InvoiceRevertRequestListQuery,
  InvoiceRevertRequestResponse,
  InvoiceRevertStatusResponse,
  ProcedureListItem,
  ProcedureListQuery,
  RecentPatientBill,
  RecordPaymentRequest,
  RejectInvoiceRevertRequest,
  VoidInvoiceRequest,
} from '../../dtos';
import type { PaginationMeta } from '../../types';
import type { HttpClient } from '../httpClient';

export interface PagedInvoices {
  items: InvoiceResponse[];
  meta: PaginationMeta;
}

export interface PagedInvoiceRevertRequests {
  items: InvoiceRevertRequestResponse[];
  meta: PaginationMeta;
}

export interface PagedProcedures {
  items: ProcedureListItem[];
  meta: PaginationMeta;
}

/**
 * Typed API service for the Billing module, built on the shared HTTP client. Feature code
 * (web/mobile) calls this, never the HTTP client directly — docs/FrontendArchitecture.md §6.
 */
export class BillingApi {
  constructor(private readonly client: HttpClient) {}

  async createInvoice(request: CreateInvoiceRequest): Promise<InvoiceResponse> {
    const response = await this.client.post<InvoiceResponse>(API_ROUTES.billing.invoices.base, request);
    return response.data;
  }

  async getInvoices(query: InvoiceListQuery = {}): Promise<PagedInvoices> {
    const response = await this.client.get<InvoiceResponse[]>(API_ROUTES.billing.invoices.base, {
      query: {
        page: query.page,
        pageSize: query.pageSize,
        sort: query.sort,
        search: query.search,
        paymentStatus: query.paymentStatus,
      },
    });

    return {
      items: response.data,
      meta: response.meta as PaginationMeta,
    };
  }

  /** The latest `count` bills across every patient, composed with age/gender/contact/
   * registration-type/consultant(s) — backs the Patient Billing page's "Recent Patient Bills"
   * table. */
  /** Billed revenue per month (last `months`) and this month's revenue by billing type. */
  async getDashboardSummary(months = 6): Promise<BillingDashboardSummary> {
    const response = await this.client.get<BillingDashboardSummary>(API_ROUTES.billing.invoices.dashboardSummary, { query: { months } });
    return response.data;
  }

  async getRecentBills(count = 10): Promise<RecentPatientBill[]> {
    const response = await this.client.get<RecentPatientBill[]>(API_ROUTES.billing.invoices.recent, { query: { count } });
    return response.data;
  }

  async getInvoiceById(id: string): Promise<InvoiceResponse> {
    const response = await this.client.get<InvoiceResponse>(API_ROUTES.billing.invoices.byId(id));
    return response.data;
  }

  async getInvoicesByPatientId(patientId: string): Promise<InvoiceResponse[]> {
    const response = await this.client.get<InvoiceResponse[]>(API_ROUTES.billing.invoices.byPatientId(patientId));
    return response.data;
  }

  async recordPayment(invoiceId: string, itemId: string, request: RecordPaymentRequest): Promise<InvoiceResponse> {
    const response = await this.client.post<InvoiceResponse>(API_ROUTES.billing.invoices.recordPayment(invoiceId, itemId), request);
    return response.data;
  }

  async voidInvoice(id: string, request: VoidInvoiceRequest): Promise<InvoiceResponse> {
    const response = await this.client.post<InvoiceResponse>(API_ROUTES.billing.invoices.void(id), request);
    return response.data;
  }

  /** Whether a revert can be requested for this invoice, and every revert request on it (ADR-106). */
  async getInvoiceRevertStatus(invoiceId: string): Promise<InvoiceRevertStatusResponse> {
    const response = await this.client.get<InvoiceRevertStatusResponse>(API_ROUTES.billing.invoices.revert(invoiceId));
    return response.data;
  }

  /** Asks a Super Admin to revert an OP bill — the reason is required. */
  async requestInvoiceRevert(invoiceId: string, request: CreateInvoiceRevertRequest): Promise<InvoiceRevertRequestResponse> {
    const response = await this.client.post<InvoiceRevertRequestResponse>(API_ROUTES.billing.invoices.revertRequests(invoiceId), request);
    return response.data;
  }

  async getRevertRequests(query: InvoiceRevertRequestListQuery = {}): Promise<PagedInvoiceRevertRequests> {
    const response = await this.client.get<InvoiceRevertRequestResponse[]>(API_ROUTES.billing.revertRequests.base, {
      query: { page: query.page, pageSize: query.pageSize, status: query.status },
    });

    return {
      items: response.data,
      meta: response.meta as PaginationMeta,
    };
  }

  async getPendingRevertRequestCount(): Promise<number> {
    const response = await this.client.get<number>(API_ROUTES.billing.revertRequests.pendingCount);
    return response.data;
  }

  /** Super Admin only — approving reverts the bill. */
  async approveRevertRequest(id: string, request: ApproveInvoiceRevertRequest = {}): Promise<InvoiceRevertRequestResponse> {
    const response = await this.client.post<InvoiceRevertRequestResponse>(API_ROUTES.billing.revertRequests.approve(id), request);
    return response.data;
  }

  /** Super Admin only — notes are required. */
  async rejectRevertRequest(id: string, request: RejectInvoiceRevertRequest): Promise<InvoiceRevertRequestResponse> {
    const response = await this.client.post<InvoiceRevertRequestResponse>(API_ROUTES.billing.revertRequests.reject(id), request);
    return response.data;
  }

  /** Backs the OPD Procedures List tab — one row per Procedure invoice line item, across
   * every invoice, filterable by date range/department/consultant/payment status. */
  async getProcedureLineItems(query: ProcedureListQuery = {}): Promise<PagedProcedures> {
    const response = await this.client.get<ProcedureListItem[]>(API_ROUTES.billing.invoices.procedures, {
      query: {
        page: query.page,
        pageSize: query.pageSize,
        search: query.search,
        from: query.from,
        to: query.to,
        departmentId: query.departmentId,
        consultantId: query.consultantId,
        paymentStatus: query.paymentStatus,
      },
    });

    return {
      items: response.data,
      meta: response.meta as PaginationMeta,
    };
  }
}
