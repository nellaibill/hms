import { API_ROUTES } from '../../constants';
import type {
  AvailableBatchResponse,
  CreateDispenseCartRequest,
  CreateStoreRequest,
  CreateDispenseRequest,
  CreateStockReceiptRequest,
  DispenseCartResponse,
  DispenseListQuery,
  DispenseResponse,
  StockBalanceListQuery,
  StockBalanceResponse,
  StockLedgerListQuery,
  StockReceiptListQuery,
  StockReceiptResponse,
  StockSummaryListQuery,
  StockSummaryResponse,
  StockTransactionResponse,
  StoreResponse,
  UpdateStoreRequest,
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

export interface PagedStockSummaries {
  items: StockSummaryResponse[];
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

  /** Every store, the default first. Unpaged — a hospital has a handful. */
  async getStores(): Promise<StoreResponse[]> {
    const response = await this.client.get<StoreResponse[]>(API_ROUTES.pharmacy.stores.base);
    return response.data;
  }

  async createStore(request: CreateStoreRequest): Promise<StoreResponse> {
    const response = await this.client.post<StoreResponse>(API_ROUTES.pharmacy.stores.base, request);
    return response.data;
  }

  async updateStore(id: string, request: UpdateStoreRequest): Promise<StoreResponse> {
    const response = await this.client.put<StoreResponse>(API_ROUTES.pharmacy.stores.byId(id), request);
    return response.data;
  }

  async getStockReceipts(query: StockReceiptListQuery = {}): Promise<PagedStockReceipts> {
    const response = await this.client.get<StockReceiptResponse[]>(API_ROUTES.pharmacy.stockReceipts.base, {
      query: {
        page: query.page,
        pageSize: query.pageSize,
        sort: query.sort,
        search: query.search,
        productId: query.productId,
        storeId: query.storeId,
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
        storeId: query.storeId,
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

  async createDispenseCart(request: CreateDispenseCartRequest): Promise<DispenseCartResponse> {
    const response = await this.client.post<DispenseCartResponse>(API_ROUTES.pharmacy.dispenses.cart, request);
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
        storeId: query.storeId,
        inStockOnly: query.inStockOnly,
      },
    });
    return { items: response.data, meta: response.meta as PaginationMeta };
  }

  /** Stock per product (every batch rolled up), name-ordered and searchable server-side. */
  async getStockSummary(query: StockSummaryListQuery = {}): Promise<PagedStockSummaries> {
    const response = await this.client.get<StockSummaryResponse[]>(API_ROUTES.pharmacy.stockBalances.summary, {
      query: {
        page: query.page,
        pageSize: query.pageSize,
        search: query.search,
        filter: query.filter,
        storeId: query.storeId,
      },
    });
    return { items: response.data, meta: response.meta as PaginationMeta };
  }

  /** The batches of a product that can be dispensed right now from one store (the default when
   * none is named), earliest expiry first, each with its unit price. */
  async getAvailableBatches(productId: string, storeId?: string): Promise<AvailableBatchResponse[]> {
    const response = await this.client.get<AvailableBatchResponse[]>(API_ROUTES.pharmacy.stockBalances.availableBatches(productId), {
      query: { storeId },
    });
    return response.data;
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
        storeId: query.storeId,
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
