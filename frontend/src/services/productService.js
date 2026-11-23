import apiClient, { extractData, isMockEnabled } from './api';
import { mockDb } from './mockData';
import { getUnitId, getUnitName, TransactionType } from '../utils/constants';

export const productService = {
  async getProducts(params = {}) {
    if (!isMockEnabled()) {
      try {
        const response = await apiClient.get('/products', { params });
        const data = extractData(response);
        // Handle both PagedResult<T> and direct array
        if (data?.items && Array.isArray(data.items)) {
          return {
            items: data.items.map(normalizeProduct),
            totalCount: data.totalCount ?? data.items.length,
            pageNumber: data.pageNumber ?? 1,
            pageSize: data.pageSize ?? 20,
            totalPages: data.totalPages ?? 1,
          };
        }
        if (Array.isArray(data)) {
          return {
            items: data.map(normalizeProduct),
            totalCount: data.length,
            pageNumber: 1,
            pageSize: data.length,
            totalPages: 1,
          };
        }
      } catch (err) {
        console.warn('GET /products failed, reading dynamic store:', err);
      }
    }

    await new Promise((r) => setTimeout(r, 150));
    let products = mockDb.getProducts().map(normalizeProduct);

    // Apply filters in dynamic store
    if (params.search) {
      const q = params.search.toLowerCase();
      products = products.filter(
        (p) => p.name.toLowerCase().includes(q) || p.sku.toLowerCase().includes(q)
      );
    }
    if (params.categoryId) {
      products = products.filter((p) => String(p.categoryId) === String(params.categoryId));
    }
    if (params.lowStockOnly) {
      products = products.filter((p) => p.currentStock <= p.minimumStock);
    }
    if (params.isActive !== undefined) {
      products = products.filter((p) => Boolean(p.isActive) === Boolean(params.isActive));
    }

    const pageNumber = Number(params.pageNumber) || 1;
    const pageSize = Number(params.pageSize) || 20;
    const totalCount = products.length;
    const totalPages = Math.ceil(totalCount / pageSize) || 1;
    const startIndex = (pageNumber - 1) * pageSize;
    const items = products.slice(startIndex, startIndex + pageSize);

    return {
      items,
      totalCount,
      pageNumber,
      pageSize,
      totalPages,
    };
  },

  async getLowStockProducts() {
    if (!isMockEnabled()) {
      try {
        const response = await apiClient.get('/products/low-stock');
        const data = extractData(response);
        if (Array.isArray(data)) return data.map(normalizeProduct);
        if (data?.items) return data.items.map(normalizeProduct);
      } catch (err) {
        console.warn('GET /products/low-stock failed:', err);
      }
    }
    const products = mockDb.getProducts().map(normalizeProduct);
    return products.filter((p) => p.currentStock <= p.minimumStock);
  },

  async getProductById(id) {
    if (!isMockEnabled()) {
      try {
        const response = await apiClient.get(`/products/${id}`);
        const data = extractData(response);
        if (data) return normalizeProduct(data);
      } catch (err) {
        console.warn(`GET /products/${id} failed:`, err);
      }
    }
    const products = mockDb.getProducts().map(normalizeProduct);
    const product = products.find((p) => String(p.id) === String(id));
    if (!product) throw new Error('Product not found');
    return product;
  },

  async createProduct(productData) {
    const unitId = getUnitId(productData.unit);
    const payload = {
      categoryId: productData.categoryId,
      name: productData.name,
      sku: productData.sku,
      description: productData.description || '',
      purchasePrice: Number(productData.purchasePrice) || 0,
      sellingPrice: Number(productData.sellingPrice) || 0,
      currentStock: Number(productData.currentStock) || 0,
      minimumStock: Number(productData.minimumStock ?? productData.minStock) || 5,
      unit: unitId,
    };

    if (!isMockEnabled()) {
      try {
        const response = await apiClient.post('/products', payload);
        const created = extractData(response);
        return normalizeProduct(created);
      } catch (err) {
        console.warn('POST /products failed:', err);
        throw err;
      }
    }

    await new Promise((r) => setTimeout(r, 200));
    const products = mockDb.getProducts();
    const categories = mockDb.getCategories();
    const cat = categories.find((c) => String(c.id) === String(productData.categoryId));

    const newProduct = {
      id: `prod-${Date.now()}`,
      ...payload,
      categoryName: cat ? cat.name : 'General',
      unitName: getUnitName(unitId),
      isActive: true,
      isLowStock: payload.currentStock <= payload.minimumStock && payload.currentStock > 0,
      isOutOfStock: payload.currentStock === 0,
      avgDailySales: 1.5,
    };

    products.unshift(newProduct);
    mockDb.saveProducts(products);

    // Record Opening Stock transaction if currentStock > 0
    if (newProduct.currentStock > 0) {
      const transactions = mockDb.getTransactions();
      transactions.unshift({
        id: `tx-${Date.now()}`,
        productId: newProduct.id,
        productName: newProduct.name,
        sku: newProduct.sku,
        transactionType: TransactionType.Opening,
        transactionTypeName: 'Opening',
        quantity: newProduct.currentStock,
        previousStock: 0,
        newStock: newProduct.currentStock,
        referenceId: 'opening-initial',
        notes: 'Initial inventory setup',
        createdAt: new Date().toISOString(),
      });
      mockDb.saveTransactions(transactions);
    }

    return newProduct;
  },

  async updateProduct(id, productData) {
    const unitId = getUnitId(productData.unit);
    const payload = {
      categoryId: productData.categoryId,
      name: productData.name,
      sku: productData.sku,
      description: productData.description || '',
      purchasePrice: Number(productData.purchasePrice) || 0,
      sellingPrice: Number(productData.sellingPrice) || 0,
      minimumStock: Number(productData.minimumStock ?? productData.minStock) || 5,
      unit: unitId,
      isActive: productData.isActive !== undefined ? productData.isActive : true,
    };

    if (!isMockEnabled()) {
      try {
        const response = await apiClient.put(`/products/${id}`, payload);
        const updated = extractData(response);
        return normalizeProduct(updated);
      } catch (err) {
        console.warn(`PUT /products/${id} failed:`, err);
        throw err;
      }
    }

    await new Promise((r) => setTimeout(r, 200));
    const products = mockDb.getProducts();
    const index = products.findIndex((p) => String(p.id) === String(id));
    if (index === -1) throw new Error('Product not found');

    const categories = mockDb.getCategories();
    const cat = categories.find((c) => String(c.id) === String(productData.categoryId));

    const updated = {
      ...products[index],
      ...payload,
      categoryName: cat ? cat.name : products[index].categoryName,
      unitName: getUnitName(unitId),
    };

    products[index] = updated;
    mockDb.saveProducts(products);
    return normalizeProduct(updated);
  },

  async deleteProduct(id) {
    if (!isMockEnabled()) {
      try {
        const response = await apiClient.delete(`/products/${id}`);
        return extractData(response);
      } catch (err) {
        console.warn(`DELETE /products/${id} failed:`, err);
        throw err;
      }
    }

    await new Promise((r) => setTimeout(r, 150));
    let products = mockDb.getProducts();
    products = products.filter((p) => String(p.id) !== String(id));
    mockDb.saveProducts(products);
    return { success: true };
  },
};

function normalizeProduct(p) {
  if (!p) return p;
  const unitName = p.unitName || getUnitName(p.unit);
  const minStock = p.minimumStock ?? p.minStock ?? 5;
  const stock = p.currentStock ?? 0;
  return {
    ...p,
    minimumStock: minStock,
    minStock: minStock,
    unit: p.unit || getUnitId(unitName),
    unitName,
    isLowStock: p.isLowStock !== undefined ? p.isLowStock : stock <= minStock && stock > 0,
    isOutOfStock: p.isOutOfStock !== undefined ? p.isOutOfStock : stock === 0,
  };
}

export default productService;
