import { API_ROUTES } from '../../constants';
import type { CreateProductRequest, Product, ProductBatch, ProductBatchListQuery, ProductListQuery, UpdateProductRequest } from '../../dtos';
import type { PaginationMeta } from '../../types';
import type { HttpClient } from '../httpClient';

export interface PagedProducts {
  items: Product[];
  meta: PaginationMeta;
}

export interface PagedProductBatches {
  items: ProductBatch[];
  meta: PaginationMeta;
}

/**
 * Typed API service for the Products module (core Product CRUD only — the 7 child
 * sub-resources under /api/v1/products/{productId}/* are a future pass), built on the
 * shared HTTP client. Mirrors UsersApi's shape.
 */
export class ProductsApi {
  constructor(private readonly client: HttpClient) {}

  async getProducts(query: ProductListQuery = {}): Promise<PagedProducts> {
    const response = await this.client.get<Product[]>(API_ROUTES.products.base, {
      query: {
        page: query.page,
        pageSize: query.pageSize,
        sort: query.sort,
        search: query.search,
        isActive: query.isActive,
        categoryId: query.categoryId,
        brandId: query.brandId,
      },
    });
    return {
      items: response.data,
      meta: response.meta as PaginationMeta,
    };
  }

  async getProductById(id: string): Promise<Product> {
    const response = await this.client.get<Product>(API_ROUTES.products.byId(id));
    return response.data;
  }

  async createProduct(request: CreateProductRequest): Promise<Product> {
    const response = await this.client.post<Product>(API_ROUTES.products.base, request);
    return response.data;
  }

  async updateProduct(id: string, request: UpdateProductRequest): Promise<Product> {
    const response = await this.client.put<Product>(API_ROUTES.products.byId(id), request);
    return response.data;
  }

  /**
   * Lists a product's batches (HMS.Modules.Products.Endpoints.ProductBatchesController) —
   * needed so Pharmacy's Dispense/Stock-Receipt forms can populate a batch dropdown once a
   * product is chosen.
   */
  async getProductBatches(productId: string, query: ProductBatchListQuery = {}): Promise<PagedProductBatches> {
    const response = await this.client.get<ProductBatch[]>(API_ROUTES.products.batches(productId), {
      query: {
        page: query.page,
        pageSize: query.pageSize,
        sort: query.sort,
        search: query.search,
        isActive: query.isActive,
      },
    });
    return { items: response.data, meta: response.meta as PaginationMeta };
  }
}
