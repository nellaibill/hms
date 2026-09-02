import { API_ROUTES } from '../../constants';
import type {
  CollectSampleRequest,
  LabDashboardSummaryResponse,
  LabOrderListQuery,
  LabOrderResponse,
  RejectForCorrectionRequest,
  RejectSampleRequest,
  SaveResultDraftRequest,
} from '../../dtos';
import type { PaginationMeta } from '../../types';
import type { HttpClient } from '../httpClient';

export interface PagedLabOrders {
  items: LabOrderResponse[];
  meta: PaginationMeta;
}

/**
 * Typed API service for the Laboratory module, built on the shared HTTP client. Feature code
 * (web/mobile) calls this, never the HTTP client directly — docs/FrontendArchitecture.md §6.
 * Mirrors HMS.Modules.Laboratory.Endpoints.LabOrdersController's exact route list — no
 * create-order method, since orders are only ever created in-process by Billing.
 */
export class LaboratoryApi {
  constructor(private readonly client: HttpClient) {}

  async getOrders(query: LabOrderListQuery = {}): Promise<PagedLabOrders> {
    const response = await this.client.get<LabOrderResponse[]>(API_ROUTES.laboratory.orders.base, {
      query: {
        page: query.page,
        pageSize: query.pageSize,
        sort: query.sort,
        search: query.search,
        status: query.status,
        priority: query.priority,
        dateFrom: query.dateFrom,
        dateTo: query.dateTo,
      },
    });

    return {
      items: response.data,
      meta: response.meta as PaginationMeta,
    };
  }

  async getDashboardSummary(): Promise<LabDashboardSummaryResponse> {
    const response = await this.client.get<LabDashboardSummaryResponse>(API_ROUTES.laboratory.orders.dashboardSummary);
    return response.data;
  }

  async getOrderById(id: string): Promise<LabOrderResponse> {
    const response = await this.client.get<LabOrderResponse>(API_ROUTES.laboratory.orders.byId(id));
    return response.data;
  }

  async getOrdersByPatientId(patientId: string): Promise<LabOrderResponse[]> {
    const response = await this.client.get<LabOrderResponse[]>(API_ROUTES.laboratory.orders.byPatientId(patientId));
    return response.data;
  }

  async collectSample(itemId: string, request: CollectSampleRequest): Promise<LabOrderResponse> {
    const response = await this.client.post<LabOrderResponse>(API_ROUTES.laboratory.orders.collectSample(itemId), request);
    return response.data;
  }

  async rejectSample(itemId: string, request: RejectSampleRequest): Promise<LabOrderResponse> {
    const response = await this.client.post<LabOrderResponse>(API_ROUTES.laboratory.orders.rejectSample(itemId), request);
    return response.data;
  }

  async requestRecollection(itemId: string): Promise<LabOrderResponse> {
    const response = await this.client.post<LabOrderResponse>(API_ROUTES.laboratory.orders.recollect(itemId));
    return response.data;
  }

  async receiveSample(itemId: string): Promise<LabOrderResponse> {
    const response = await this.client.post<LabOrderResponse>(API_ROUTES.laboratory.orders.receive(itemId));
    return response.data;
  }

  async startProcessing(itemId: string): Promise<LabOrderResponse> {
    const response = await this.client.post<LabOrderResponse>(API_ROUTES.laboratory.orders.startProcessing(itemId));
    return response.data;
  }

  async saveResultDraft(itemId: string, request: SaveResultDraftRequest): Promise<LabOrderResponse> {
    const response = await this.client.put<LabOrderResponse>(API_ROUTES.laboratory.orders.resultDraft(itemId), request);
    return response.data;
  }

  async submitForVerification(itemId: string): Promise<LabOrderResponse> {
    const response = await this.client.post<LabOrderResponse>(API_ROUTES.laboratory.orders.submitForVerification(itemId));
    return response.data;
  }

  async verify(itemId: string): Promise<LabOrderResponse> {
    const response = await this.client.post<LabOrderResponse>(API_ROUTES.laboratory.orders.verify(itemId));
    return response.data;
  }

  async rejectForCorrection(itemId: string, request: RejectForCorrectionRequest): Promise<LabOrderResponse> {
    const response = await this.client.post<LabOrderResponse>(API_ROUTES.laboratory.orders.rejectForCorrection(itemId), request);
    return response.data;
  }

  async generateReport(orderId: string): Promise<LabOrderResponse> {
    const response = await this.client.post<LabOrderResponse>(API_ROUTES.laboratory.orders.generateReport(orderId));
    return response.data;
  }

  async releaseReport(orderId: string): Promise<LabOrderResponse> {
    const response = await this.client.post<LabOrderResponse>(API_ROUTES.laboratory.orders.releaseReport(orderId));
    return response.data;
  }
}
