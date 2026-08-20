import { API_ROUTES } from '../../constants';
import type {
  CreateDispenseRequest,
  CreateStockReceiptRequest,
  DispenseListQuery,
  DispenseResponse,
  StockBalanceListQuery,
  StockBalanceResponse,
  StockLedgerListQuery,
  StockReceiptListQuery,
  StockReceiptResponse,
  StockTransactionResponse,
} from '../../dtos';
import type { PaginationMeta } from '../../types';
import type { HttpClient } from '../httpClient';

export interface PagedStockReceipts {
  items: StockReceiptResponse[];
  meta: PaginationMeta;
}

export interface PagedDispenses {
  items: DispenseResponse[];
  meta: PaginationMeta;
}

export interface PagedStockBalances {
  items: StockBalanceResponse[];
  meta: PaginationMeta;
}

export interface PagedStockTransactions {
  items: StockTransactionResponse[];
  meta: PaginationMeta;
}

/**
 * Typed API service for the Pharmacy module (stock receipts, direct-dispense, current
 * stock balances, and the combined stock ledger), built on the shared HTTP client.
 * Mirrors AdmissionsApi's shape (one class covering several related controllers). No
 * PUT/DELETE anywhere — every list is append-only history, so no edit/delete methods either.
 */
export class PharmacyApi {
  constructor(private readonly client: HttpClient) {}

  async getStockReceipts(query: StockReceiptListQuery = {}): Promise<PagedStockReceipts> {
    const response = await this.client.get<StockReceiptResponse[]>(API_ROUTES.pharmacy.stockReceipts.base, {
      query: {
        page: query.page,
        pageSize: query.pageSize,
        sort: query.sort,
        search: query.search,
        productId: query.productId,
      },
    });
    return { items: response.data, meta: response.meta as PaginationMeta };
  }

  async getStockReceiptById(id: string): Promise<StockReceiptResponse> {
    const response = await this.client.get<StockReceiptResponse>(API_ROUTES.pharmacy.stockReceipts.byId(id));
    return response.data;
  }

  async createStockReceipt(request: CreateStockReceiptRequest): Promise<StockReceiptResponse> {
    const response = await this.client.post<StockReceiptResponse>(API_ROUTES.pharmacy.stockReceipts.base, request);
    return response.data;
  }

  async getDispenses(query: DispenseListQuery = {}): Promise<PagedDispenses> {
    const response = await this.client.get<DispenseResponse[]>(API_ROUTES.pharmacy.dispenses.base, {
      query: {
        page: query.page,
        pageSize: query.pageSize,
        sort: query.sort,
        search: query.search,
        patientId: query.patientId,
        productId: query.productId,
      },
    });
    return { items: response.data, meta: response.meta as PaginationMeta };
  }

  async getDispenseById(id: string): Promise<DispenseResponse> {
    const response = await this.client.get<DispenseResponse>(API_ROUTES.pharmacy.dispenses.byId(id));
    return response.data;
  }

  async createDispense(request: CreateDispenseRequest): Promise<DispenseResponse> {
    const response = await this.client.post<DispenseResponse>(API_ROUTES.pharmacy.dispenses.base, request);
    return response.data;
  }

  async getStockBalances(query: StockBalanceListQuery = {}): Promise<PagedStockBalances> {
    const response = await this.client.get<StockBalanceResponse[]>(API_ROUTES.pharmacy.stockBalances.base, {
      query: {
        page: query.page,
        pageSize: query.pageSize,
        sort: query.sort,
        search: query.search,
        productId: query.productId,
      },
    });
    return { items: response.data, meta: response.meta as PaginationMeta };
  }

  async getStockBalance(productId: string, productBatchId: string): Promise<StockBalanceResponse> {
    const response = await this.client.get<StockBalanceResponse>(API_ROUTES.pharmacy.stockBalances.byProductBatch(productId, productBatchId));
    return response.data;
  }

  async getStockLedger(query: StockLedgerListQuery = {}): Promise<PagedStockTransactions> {
    const response = await this.client.get<StockTransactionResponse[]>(API_ROUTES.pharmacy.stockLedger, {
      query: {
        page: query.page,
        pageSize: query.pageSize,
        sort: query.sort,
        search: query.search,
        productId: query.productId,
        productBatchId: query.productBatchId,
        transactionType: query.transactionType,
        patientId: query.patientId,
        fromDate: query.fromDate,
        toDate: query.toDate,
      },
    });
    return { items: response.data, meta: response.meta as PaginationMeta };
  }
}
