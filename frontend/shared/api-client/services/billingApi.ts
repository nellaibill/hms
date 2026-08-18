import { API_ROUTES } from '../../constants';
import type { CreateInvoiceRequest, InvoiceListQuery, InvoiceResponse, RecordPaymentRequest } from '../../dtos';
import type { PaginationMeta } from '../../types';
import type { HttpClient } from '../httpClient';

export interface PagedInvoices {
  items: InvoiceResponse[];
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
}
