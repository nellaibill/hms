import type { CreateProductRequest, PagedProducts, Product, ProductListQuery, UpdateProductRequest } from '@hms/shared';
import { MOCK_PRODUCTS } from './mockProducts';

/**
 * Offline fallback store used only when the real API is unreachable (see the NetworkError
 * catch in the products hooks) — mirrors features/patients/mockPatientsStore.ts. Persisted
 * to localStorage so it survives page refreshes during a demo.
 */
const STORAGE_KEY = 'hms-mock-products';

function loadProducts(): Product[] {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (raw) {
      const parsed = JSON.parse(raw) as Product[];
      if (Array.isArray(parsed) && parsed.length > 0) {
        return parsed;
      }
    }
  } catch {
    // Corrupt/unavailable storage — fall through to seed data.
  }
  return [...MOCK_PRODUCTS];
}

function persist() {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(products));
  } catch {
    // Storage unavailable (e.g. private browsing quota) — demo still works for this tab session.
  }
}

let products: Product[] = loadProducts();
let nextSeq = products.reduce((max, p) => Math.max(max, Number(p.id.replace('mock-product-', '')) || 0), 0) + 1;

function compareBy(field: string, direction: 1 | -1) {
  return (a: Product, b: Product) => {
    let left: string | number;
    let right: string | number;
    switch (field) {
      case 'sku':
        left = a.sku;
        right = b.sku;
        break;
      case 'sellingPrice':
        left = a.sellingPrice;
        right = b.sellingPrice;
        break;
      case 'productName':
      default:
        left = a.productName;
        right = b.productName;
        break;
    }
    return typeof left === 'number' && typeof right === 'number' ? (left - right) * direction : String(left).localeCompare(String(right)) * direction;
  };
}

export function listMockProducts(query: ProductListQuery): PagedProducts {
  const page = query.page ?? 1;
  const pageSize = query.pageSize ?? 20;
  const search = query.search?.trim().toLowerCase();

  let items = products;
  if (query.isActive !== undefined) {
    items = items.filter((p) => p.isActive === query.isActive);
  }
  if (query.categoryId) {
    items = items.filter((p) => p.categoryId === query.categoryId);
  }
  if (query.brandId) {
    items = items.filter((p) => p.brandId === query.brandId);
  }
  if (search) {
    items = items.filter(
      (p) => p.productName.toLowerCase().includes(search) || p.sku.toLowerCase().includes(search) || p.productCode.toLowerCase().includes(search),
    );
  }

  const sort = query.sort ?? '-createdAt';
  const direction = sort.startsWith('-') ? -1 : 1;
  const field = sort.startsWith('-') ? sort.slice(1) : sort;
  items = [...items].sort(compareBy(field, direction));

  const totalCount = items.length;
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));
  const start = (page - 1) * pageSize;
  const pageItems = items.slice(start, start + pageSize);

  return { items: pageItems, meta: { page, pageSize, totalCount, totalPages } };
}

export function getMockProductById(id: string): Product | undefined {
  return products.find((p) => p.id === id);
}

export function createMockProduct(request: CreateProductRequest): Product {
  const seq = nextSeq++;
  const now = new Date().toISOString();
  const product: Product = {
    id: `mock-product-${String(seq).padStart(3, '0')}`,
    ...request,
    createdAt: now,
    updatedAt: null,
  };
  products = [product, ...products];
  persist();
  return product;
}

export function updateMockProduct(id: string, request: UpdateProductRequest): Product {
  const existing = getMockProductById(id);
  if (!existing) {
    throw new Error(`Mock product ${id} not found.`);
  }
  const updated: Product = { ...existing, ...request, updatedAt: new Date().toISOString() };
  products = products.map((p) => (p.id === id ? updated : p));
  persist();
  return updated;
}
