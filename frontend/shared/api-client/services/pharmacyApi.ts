import { API_ROUTES } from '../../constants';
import type {
  CollectionRow,
  DailySalesRow,
  GstSummaryResponse,
  NonMovingStockRow,
  PharmacyDashboardResponse,
  PharmacyReportPeriod,
  ProductSalesRow,
  PurchaseRegisterResponse,
  SalesRegisterResponse,
  StockValuationResponse,
  AvailableBatchResponse,
  CollectSalePaymentRequest,
  CounterItemResponse,
  CreateSaleRequest,
  CreateStockAdjustmentRequest,
  CreateStockTransferRequest,
  ExpiringBatchResponse,
  ScheduleRegisterEntry,
  StockAdjustmentListQuery,
  StockAdjustmentResponse,
  StockCountListQuery,
  StockCountResponse,
  StockTransferListQuery,
  StockTransferResponse,
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
  CreateIndentRequest,
  IndentListQuery,
  IndentResponse,
  PrescriptionQueueItemResponse,
  CreatePurchaseReturnRequest,
  CreateSupplierPaymentRequest,
  PurchaseOrderListQuery,
  PurchaseOrderResponse,
  PurchaseReturnListQuery,
  PurchaseReturnResponse,
  ReorderSuggestionResponse,
  ReturnableBatchResponse,
  SavePurchaseOrderRequest,
  SupplierBalanceResponse,
  SupplierPaymentResponse,
  SupplierStatementResponse,
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

  /** Products at or below their reorder level, with a suggested order quantity. */
  async getReorderSuggestions(storeId?: string): Promise<ReorderSuggestionResponse[]> {
    const response = await this.client.get<ReorderSuggestionResponse[]>(API_ROUTES.pharmacy.purchaseOrders.suggestions, { query: { storeId } });
    return response.data;
  }

  async getPurchaseOrders(query: PurchaseOrderListQuery = {}): Promise<Paged<PurchaseOrderResponse>> {
    const response = await this.client.get<PurchaseOrderResponse[]>(API_ROUTES.pharmacy.purchaseOrders.base, { query: { ...query } });
    return { items: response.data, meta: response.meta as PaginationMeta };
  }

  async getPurchaseOrderById(id: string): Promise<PurchaseOrderResponse> {
    const response = await this.client.get<PurchaseOrderResponse>(API_ROUTES.pharmacy.purchaseOrders.byId(id));
    return response.data;
  }

  async createPurchaseOrder(request: SavePurchaseOrderRequest): Promise<PurchaseOrderResponse> {
    const response = await this.client.post<PurchaseOrderResponse>(API_ROUTES.pharmacy.purchaseOrders.base, request);
    return response.data;
  }

  /** Replaces a draft order. A sent order can no longer be edited. */
  async updatePurchaseOrder(id: string, request: SavePurchaseOrderRequest): Promise<PurchaseOrderResponse> {
    const response = await this.client.put<PurchaseOrderResponse>(API_ROUTES.pharmacy.purchaseOrders.byId(id), request);
    return response.data;
  }

  async placePurchaseOrder(id: string): Promise<PurchaseOrderResponse> {
    const response = await this.client.post<PurchaseOrderResponse>(API_ROUTES.pharmacy.purchaseOrders.place(id));
    return response.data;
  }

  async cancelPurchaseOrder(id: string): Promise<PurchaseOrderResponse> {
    const response = await this.client.post<PurchaseOrderResponse>(API_ROUTES.pharmacy.purchaseOrders.cancel(id));
    return response.data;
  }

  /** The batches of a product a store holds and could send back, expired ones included. */
  async getReturnableBatches(productId: string, storeId?: string): Promise<ReturnableBatchResponse[]> {
    const response = await this.client.get<ReturnableBatchResponse[]>(API_ROUTES.pharmacy.purchaseReturns.returnableBatches(productId), { query: { storeId } });
    return response.data;
  }

  async createPurchaseReturn(request: CreatePurchaseReturnRequest): Promise<PurchaseReturnResponse> {
    const response = await this.client.post<PurchaseReturnResponse>(API_ROUTES.pharmacy.purchaseReturns.base, request);
    return response.data;
  }

  async getPurchaseReturns(query: PurchaseReturnListQuery = {}): Promise<Paged<PurchaseReturnResponse>> {
    const response = await this.client.get<PurchaseReturnResponse[]>(API_ROUTES.pharmacy.purchaseReturns.base, { query: { ...query } });
    return { items: response.data, meta: response.meta as PaginationMeta };
  }

  async getPurchaseReturnById(id: string): Promise<PurchaseReturnResponse> {
    const response = await this.client.get<PurchaseReturnResponse>(API_ROUTES.pharmacy.purchaseReturns.byId(id));
    return response.data;
  }

  /** Every supplier the pharmacy has dealt with: bought, returned, paid, outstanding. */
  async getSupplierBalances(): Promise<SupplierBalanceResponse[]> {
    const response = await this.client.get<SupplierBalanceResponse[]>(API_ROUTES.pharmacy.supplierAccounts.base);
    return response.data;
  }

  async getSupplierStatement(supplierId: string, fromDate?: string, toDate?: string): Promise<SupplierStatementResponse> {
    const response = await this.client.get<SupplierStatementResponse>(API_ROUTES.pharmacy.supplierAccounts.statement(supplierId), { query: { fromDate, toDate } });
    return response.data;
  }

  async recordSupplierPayment(supplierId: string, request: CreateSupplierPaymentRequest): Promise<SupplierPaymentResponse> {
    const response = await this.client.post<SupplierPaymentResponse>(API_ROUTES.pharmacy.supplierAccounts.payments(supplierId), request);
    return response.data;
  }

  /** Completed OPD consultations with medicines prescribed on the given days (today by default). */
  async getPrescriptionQueue(fromDate?: string, toDate?: string): Promise<PrescriptionQueueItemResponse[]> {
    const response = await this.client.get<PrescriptionQueueItemResponse[]>(API_ROUTES.pharmacy.prescriptions, { query: { fromDate, toDate } });
    return response.data;
  }

  /** The pharmacy's catalog as clinical staff may search it: items and what is in stock, no cost. */
  async searchCatalog(search: string, storeId?: string, limit = 15): Promise<CounterItemResponse[]> {
    const response = await this.client.get<CounterItemResponse[]>(API_ROUTES.pharmacy.catalog, { query: { search, storeId, limit } });
    return response.data;
  }

  async getIndents(query: IndentListQuery = {}): Promise<Paged<IndentResponse>> {
    const response = await this.client.get<IndentResponse[]>(API_ROUTES.pharmacy.indents.base, { query: { ...query } });
    return { items: response.data, meta: response.meta as PaginationMeta };
  }

  async getIndentById(id: string): Promise<IndentResponse> {
    const response = await this.client.get<IndentResponse>(API_ROUTES.pharmacy.indents.byId(id));
    return response.data;
  }

  /** Raises a ward's request for an admitted patient's medicines. */
  async createIndent(request: CreateIndentRequest): Promise<IndentResponse> {
    const response = await this.client.post<IndentResponse>(API_ROUTES.pharmacy.indents.base, request);
    return response.data;
  }

  async cancelIndent(id: string): Promise<IndentResponse> {
    const response = await this.client.post<IndentResponse>(API_ROUTES.pharmacy.indents.cancel(id));
    return response.data;
  }

  /** Corrects stock, with a reason. Positive quantities add; negative take away. */
  async createStockAdjustment(request: CreateStockAdjustmentRequest): Promise<StockAdjustmentResponse> {
    const response = await this.client.post<StockAdjustmentResponse>(API_ROUTES.pharmacy.stockControl.adjustments, request);
    return response.data;
  }

  async getStockAdjustments(query: StockAdjustmentListQuery = {}): Promise<Paged<StockAdjustmentResponse>> {
    const response = await this.client.get<StockAdjustmentResponse[]>(API_ROUTES.pharmacy.stockControl.adjustments, { query: { ...query } });
    return { items: response.data, meta: response.meta as PaginationMeta };
  }

  async getStockAdjustmentById(id: string): Promise<StockAdjustmentResponse> {
    const response = await this.client.get<StockAdjustmentResponse>(API_ROUTES.pharmacy.stockControl.adjustmentById(id));
    return response.data;
  }

  /** Moves stock from one store to another, in one step. */
  async createStockTransfer(request: CreateStockTransferRequest): Promise<StockTransferResponse> {
    const response = await this.client.post<StockTransferResponse>(API_ROUTES.pharmacy.stockControl.transfers, request);
    return response.data;
  }

  async getStockTransfers(query: StockTransferListQuery = {}): Promise<Paged<StockTransferResponse>> {
    const response = await this.client.get<StockTransferResponse[]>(API_ROUTES.pharmacy.stockControl.transfers, { query: { ...query } });
    return { items: response.data, meta: response.meta as PaginationMeta };
  }

  async getStockTransferById(id: string): Promise<StockTransferResponse> {
    const response = await this.client.get<StockTransferResponse>(API_ROUTES.pharmacy.stockControl.transferById(id));
    return response.data;
  }

  /** Starts a physical count of a store (the default store when none is named). */
  async startStockCount(storeId?: string, remarks?: string): Promise<StockCountResponse> {
    const response = await this.client.post<StockCountResponse>(API_ROUTES.pharmacy.stockControl.counts, { storeId, remarks });
    return response.data;
  }

  async saveStockCount(id: string, entries: { lineId: string; countedQuantity: number | null }[]): Promise<StockCountResponse> {
    const response = await this.client.put<StockCountResponse>(API_ROUTES.pharmacy.stockControl.countById(id), { entries });
    return response.data;
  }

  /** Posts the count: every difference becomes one stock adjustment. */
  async postStockCount(id: string): Promise<StockCountResponse> {
    const response = await this.client.post<StockCountResponse>(API_ROUTES.pharmacy.stockControl.postCount(id));
    return response.data;
  }

  async cancelStockCount(id: string): Promise<StockCountResponse> {
    const response = await this.client.post<StockCountResponse>(API_ROUTES.pharmacy.stockControl.cancelCount(id));
    return response.data;
  }

  async getStockCounts(query: StockCountListQuery = {}): Promise<Paged<StockCountResponse>> {
    const response = await this.client.get<StockCountResponse[]>(API_ROUTES.pharmacy.stockControl.counts, { query: { ...query } });
    return { items: response.data, meta: response.meta as PaginationMeta };
  }

  async getStockCountById(id: string): Promise<StockCountResponse> {
    const response = await this.client.get<StockCountResponse>(API_ROUTES.pharmacy.stockControl.countById(id));
    return response.data;
  }

  /** Batches that have expired or will within `days` (the pharmacy's alert window when omitted). */
  async getExpiringBatches(storeId?: string, days?: number): Promise<ExpiringBatchResponse[]> {
    const response = await this.client.get<ExpiringBatchResponse[]>(API_ROUTES.pharmacy.stockControl.expiring, { query: { storeId, days } });
    return response.data;
  }

  /** Every sale of a scheduled drug in a period (H1, X and narcotic unless `schedules` says otherwise). */
  async getScheduleRegister(fromDate: string, toDate: string, schedules?: string[], storeId?: string): Promise<ScheduleRegisterEntry[]> {
    const params = new URLSearchParams({ fromDate, toDate });
    schedules?.forEach((schedule) => params.append('schedule', schedule));
    if (storeId) params.set('storeId', storeId);
    const response = await this.client.get<ScheduleRegisterEntry[]>(`${API_ROUTES.pharmacy.stockControl.scheduleRegister}?${params.toString()}`);
    return response.data;
  }

  // ---- Reports and dashboard (read-only; cost figures come back null without the right to see them)

  async getDashboard(storeId?: string): Promise<PharmacyDashboardResponse> {
    const response = await this.client.get<PharmacyDashboardResponse>(API_ROUTES.pharmacy.reports.dashboard, { query: { storeId } });
    return response.data;
  }

  async getDailySalesReport(period: PharmacyReportPeriod): Promise<DailySalesRow[]> {
    const response = await this.client.get<DailySalesRow[]>(API_ROUTES.pharmacy.reports.dailySales, { query: { ...period } });
    return response.data;
  }

  /** Every bill and sale return in a period of up to three months. */
  async getSalesRegister(period: PharmacyReportPeriod): Promise<SalesRegisterResponse> {
    const response = await this.client.get<SalesRegisterResponse>(API_ROUTES.pharmacy.reports.salesRegister, { query: { ...period } });
    return response.data;
  }

  async getProductSalesReport(period: PharmacyReportPeriod): Promise<ProductSalesRow[]> {
    const response = await this.client.get<ProductSalesRow[]>(API_ROUTES.pharmacy.reports.productSales, { query: { ...period } });
    return response.data;
  }

  async getCollectionsReport(period: PharmacyReportPeriod): Promise<CollectionRow[]> {
    const response = await this.client.get<CollectionRow[]>(API_ROUTES.pharmacy.reports.collections, { query: { ...period } });
    return response.data;
  }

  async getGstSummary(period: PharmacyReportPeriod): Promise<GstSummaryResponse> {
    const response = await this.client.get<GstSummaryResponse>(API_ROUTES.pharmacy.reports.gst, { query: { ...period } });
    return response.data;
  }

  async getPurchaseRegister(period: PharmacyReportPeriod): Promise<PurchaseRegisterResponse> {
    const response = await this.client.get<PurchaseRegisterResponse>(API_ROUTES.pharmacy.reports.purchaseRegister, { query: { ...period } });
    return response.data;
  }

  async getStockValuation(storeId?: string): Promise<StockValuationResponse> {
    const response = await this.client.get<StockValuationResponse>(API_ROUTES.pharmacy.reports.stockValuation, { query: { storeId } });
    return response.data;
  }

  /** Stock not billed for `days` days (90 when omitted). */
  async getNonMovingStock(storeId?: string, days?: number): Promise<NonMovingStockRow[]> {
    const response = await this.client.get<NonMovingStockRow[]>(API_ROUTES.pharmacy.reports.nonMoving, { query: { storeId, days } });
    return response.data;
  }
}
