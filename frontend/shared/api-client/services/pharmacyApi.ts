import { API_ROUTES } from '../../constants';
import type {
  AvailableBatchResponse,
  CollectSalePaymentRequest,
  CounterItemResponse,
  CreateSaleRequest,
  CreateSaleReturnRequest,
  HeldBillResponse,
  HoldBillRequest,
  PharmacySettings,
  UpdatePharmacySettingsRequest,
  SaleListQuery,
  SaleResponse,
  SaleReturnListQuery,
  SaleReturnResponse,
  CreateDispenseCartRequest,
  CreateGoodsReceiptRequest,
  GoodsReceiptListQuery,
  GoodsReceiptResponse,
  ImportProductsRequest,
  ImportProductsResponse,
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

/** A page of any pharmacy list. */
export interface Paged<T> {
  items: T[];
  meta: PaginationMeta;
}

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

  async getGoodsReceipts(query: GoodsReceiptListQuery = {}): Promise<Paged<GoodsReceiptResponse>> {
    const response = await this.client.get<GoodsReceiptResponse[]>(API_ROUTES.pharmacy.goodsReceipts.base, { query: { ...query } });
    return { items: response.data, meta: response.meta as PaginationMeta };
  }

  async getGoodsReceiptById(id: string): Promise<GoodsReceiptResponse> {
    const response = await this.client.get<GoodsReceiptResponse>(API_ROUTES.pharmacy.goodsReceipts.byId(id));
    return response.data;
  }

  /** Books a supplier invoice into stock. Posted on save — there is no draft and no edit. */
  async createGoodsReceipt(request: CreateGoodsReceiptRequest): Promise<GoodsReceiptResponse> {
    const response = await this.client.post<GoodsReceiptResponse>(API_ROUTES.pharmacy.goodsReceipts.base, request);
    return response.data;
  }

  /** Imports up to 200 spreadsheet rows; with `validateOnly`, reports what would happen without saving. */
  async importProducts(request: ImportProductsRequest): Promise<ImportProductsResponse> {
    const response = await this.client.post<ImportProductsResponse>(API_ROUTES.pharmacy.imports.products, request);
    return response.data;
  }

  /** Items matching what was typed or scanned, with their sellable batches and prices in one store. */
  async searchCounterItems(search: string, storeId?: string, limit = 20): Promise<CounterItemResponse[]> {
    const response = await this.client.get<CounterItemResponse[]>(API_ROUTES.pharmacy.counter.items, { query: { search, storeId, limit } });
    return response.data;
  }

  /** The named products with their sellable stock as it is now. */
  async lookupCounterItems(productIds: string[], storeId?: string): Promise<CounterItemResponse[]> {
    const response = await this.client.post<CounterItemResponse[]>(API_ROUTES.pharmacy.counter.itemsLookup, productIds, { query: { storeId } });
    return response.data;
  }

  /** In-stock products with the same generic. */
  async getSubstitutes(productId: string, storeId?: string): Promise<CounterItemResponse[]> {
    const response = await this.client.get<CounterItemResponse[]>(API_ROUTES.pharmacy.counter.substitutes(productId), { query: { storeId } });
    return response.data;
  }

  async holdBill(request: HoldBillRequest): Promise<HeldBillResponse> {
    const response = await this.client.post<HeldBillResponse>(API_ROUTES.pharmacy.counter.heldBills, request);
    return response.data;
  }

  async getHeldBills(storeId?: string): Promise<HeldBillResponse[]> {
    const response = await this.client.get<HeldBillResponse[]>(API_ROUTES.pharmacy.counter.heldBills, { query: { storeId } });
    return response.data;
  }

  async discardHeldBill(id: string): Promise<void> {
    await this.client.delete<boolean>(API_ROUTES.pharmacy.counter.heldBillById(id));
  }

  /** Makes a bill: stock out, GST worked out, payment recorded — in one call. */
  async createSale(request: CreateSaleRequest): Promise<SaleResponse> {
    const response = await this.client.post<SaleResponse>(API_ROUTES.pharmacy.sales.base, request);
    return response.data;
  }

  async getSales(query: SaleListQuery = {}): Promise<Paged<SaleResponse>> {
    const response = await this.client.get<SaleResponse[]>(API_ROUTES.pharmacy.sales.base, { query: { ...query } });
    return { items: response.data, meta: response.meta as PaginationMeta };
  }

  async getSaleById(id: string): Promise<SaleResponse> {
    const response = await this.client.get<SaleResponse>(API_ROUTES.pharmacy.sales.byId(id));
    return response.data;
  }

  async collectSalePayment(id: string, request: CollectSalePaymentRequest): Promise<SaleResponse> {
    const response = await this.client.post<SaleResponse>(API_ROUTES.pharmacy.sales.payments(id), request);
    return response.data;
  }

  async createSaleReturn(saleId: string, request: CreateSaleReturnRequest): Promise<SaleReturnResponse> {
    const response = await this.client.post<SaleReturnResponse>(API_ROUTES.pharmacy.sales.returnsFor(saleId), request);
    return response.data;
  }

  async getSaleReturns(query: SaleReturnListQuery = {}): Promise<Paged<SaleReturnResponse>> {
    const response = await this.client.get<SaleReturnResponse[]>(API_ROUTES.pharmacy.sales.returns, { query: { ...query } });
    return { items: response.data, meta: response.meta as PaginationMeta };
  }

  async getSettings(): Promise<PharmacySettings> {
    const response = await this.client.get<PharmacySettings>(API_ROUTES.pharmacy.settings);
    return response.data;
  }

  async updateSettings(request: UpdatePharmacySettingsRequest): Promise<PharmacySettings> {
    const response = await this.client.put<PharmacySettings>(API_ROUTES.pharmacy.settings, request);
    return response.data;
  }
}
