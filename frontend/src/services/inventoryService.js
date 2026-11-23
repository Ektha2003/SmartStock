import apiClient, { extractData, isMockEnabled } from './api';
import { mockDb } from './mockData';
import { TransactionType } from '../utils/constants';

export const inventoryService = {
  async getSummary() {
    if (!isMockEnabled()) {
      try {
        const response = await apiClient.get('/inventory/summary');
        const data = extractData(response);
        if (data) return data;
      } catch (err) {
        console.warn('GET /inventory/summary failed, computing dynamically:', err);
      }
    }

    await new Promise((r) => setTimeout(r, 150));
    const products = mockDb.getProducts();
    const totalUnits = products.reduce((acc, p) => acc + (p.currentStock || 0), 0);
    const totalCostValue = products.reduce((acc, p) => acc + (p.currentStock * p.purchasePrice || 0), 0);
    const totalRetailValue = products.reduce((acc, p) => acc + (p.currentStock * p.sellingPrice || 0), 0);
    const lowStockCount = products.filter((p) => p.currentStock <= p.minimumStock && p.currentStock > 0).length;
    const outOfStockCount = products.filter((p) => p.currentStock === 0).length;

    return {
      totalProducts: products.length,
      totalStockQuantity: totalUnits,
      totalInventoryCostValue: totalCostValue,
      totalInventoryRetailValue: totalRetailValue,
      inventoryValue: totalCostValue,
      lowStockCount,
      outOfStockCount,
    };
  },

  async getTransactions(params = {}) {
    if (!isMockEnabled()) {
      try {
        const response = await apiClient.get('/inventory/transactions', { params });
        const data = extractData(response);
        if (data?.items && Array.isArray(data.items)) {
          return {
            items: data.items.map(normalizeTransaction),
            totalCount: data.totalCount ?? data.items.length,
          };
        }
        if (Array.isArray(data)) {
          return {
            items: data.map(normalizeTransaction),
            totalCount: data.length,
          };
        }
      } catch (err) {
        console.warn('GET /inventory/transactions failed, reading dynamic store:', err);
      }
    }

    await new Promise((r) => setTimeout(r, 150));
    let transactions = mockDb.getTransactions().map(normalizeTransaction);

    if (params.productId) {
      transactions = transactions.filter((t) => String(t.productId) === String(params.productId));
    }
    if (params.type) {
      transactions = transactions.filter((t) => Number(t.transactionType) === Number(params.type));
    }

    return {
      items: transactions,
      totalCount: transactions.length,
    };
  },

  async getProductHistory(productId) {
    if (!isMockEnabled()) {
      try {
        const response = await apiClient.get(`/inventory/product/${productId}/history`);
        const data = extractData(response);
        if (Array.isArray(data)) return data.map(normalizeTransaction);
        if (data?.items) return data.items.map(normalizeTransaction);
      } catch (err) {
        console.warn(`GET /inventory/product/${productId}/history failed:`, err);
      }
    }
    const transactions = mockDb.getTransactions().map(normalizeTransaction);
    return transactions.filter((t) => String(t.productId) === String(productId));
  },

  async createAdjustment(adjustmentData) {
    const payload = {
      productId: adjustmentData.productId,
      newQuantity: Number(adjustmentData.newQuantity ?? adjustmentData.quantity),
      reason: adjustmentData.reason || 'Manual count reconciliation',
    };

    if (!isMockEnabled()) {
      try {
        const response = await apiClient.post('/inventory/adjustment', payload);
        return extractData(response);
      } catch (err) {
        console.warn('POST /inventory/adjustment failed:', err);
        throw err;
      }
    }

    await new Promise((r) => setTimeout(r, 200));
    const products = mockDb.getProducts();
    const transactions = mockDb.getTransactions();
    const product = products.find((p) => String(p.id) === String(payload.productId));

    if (!product) throw new Error('Product not found');

    const prevStock = product.currentStock;
    product.currentStock = payload.newQuantity;
    const diff = payload.newQuantity - prevStock;

    const newTx = {
      id: `tx-${Date.now()}`,
      productId: product.id,
      productName: product.name,
      sku: product.sku,
      transactionType: TransactionType.Adjustment,
      transactionTypeName: 'Adjustment',
      quantity: diff,
      previousStock: prevStock,
      newStock: payload.newQuantity,
      referenceId: 'manual-adj',
      notes: payload.reason,
      createdAt: new Date().toISOString(),
    };

    transactions.unshift(newTx);
    mockDb.saveProducts(products);
    mockDb.saveTransactions(transactions);

    return {
      success: true,
      product,
      transaction: newTx,
    };
  },
};

function normalizeTransaction(t) {
  if (!t) return t;
  return {
    ...t,
    date: t.createdAt || t.date,
    createdAt: t.createdAt || t.date,
    type: t.transactionTypeName || (t.transactionType === 3 ? 'Sale' : t.transactionType === 2 ? 'Purchase' : t.transactionType === 4 ? 'Adjustment' : 'Opening'),
    reference: t.notes || t.referenceId || t.reference || '-',
  };
}

export default inventoryService;
