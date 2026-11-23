import apiClient, { extractData, isMockEnabled } from './api';
import { mockDb } from './mockData';
import { TransactionType } from '../utils/constants';

export const purchaseService = {
  async getPurchases(params = {}) {
    if (!isMockEnabled()) {
      try {
        const response = await apiClient.get('/purchases', { params });
        const data = extractData(response);
        if (data?.items && Array.isArray(data.items)) {
          return {
            items: data.items.map(normalizePurchase),
            totalCount: data.totalCount ?? data.items.length,
            pageNumber: data.pageNumber ?? 1,
            pageSize: data.pageSize ?? 20,
            totalPages: data.totalPages ?? 1,
          };
        }
        if (Array.isArray(data)) {
          return {
            items: data.map(normalizePurchase),
            totalCount: data.length,
            pageNumber: 1,
            pageSize: data.length,
            totalPages: 1,
          };
        }
      } catch (err) {
        console.warn('GET /purchases failed, reading dynamic store:', err);
      }
    }

    await new Promise((r) => setTimeout(r, 150));
    let purchases = mockDb.getPurchases().map(normalizePurchase);

    const pageNumber = Number(params.pageNumber) || 1;
    const pageSize = Number(params.pageSize) || 20;
    const totalCount = purchases.length;
    const totalPages = Math.ceil(totalCount / pageSize) || 1;
    const startIndex = (pageNumber - 1) * pageSize;
    const items = purchases.slice(startIndex, startIndex + pageSize);

    return {
      items,
      totalCount,
      pageNumber,
      pageSize,
      totalPages,
    };
  },

  async getPurchaseById(id) {
    if (!isMockEnabled()) {
      try {
        const response = await apiClient.get(`/purchases/${id}`);
        const data = extractData(response);
        if (data) return normalizePurchase(data);
      } catch (err) {
        console.warn(`GET /purchases/${id} failed:`, err);
      }
    }
    const purchases = mockDb.getPurchases().map(normalizePurchase);
    const purchase = purchases.find((p) => String(p.id) === String(id) || p.invoiceNumber === id);
    if (!purchase) throw new Error('Purchase record not found');
    return purchase;
  },

  async createPurchase(purchaseData) {
    const payload = {
      supplierId: purchaseData.supplierId,
      purchaseDate: purchaseData.purchaseDate || new Date().toISOString(),
      notes: purchaseData.notes || '',
      items: purchaseData.items.map((i) => ({
        productId: i.productId,
        quantity: Number(i.quantity),
        unitCost: Number(i.unitCost),
      })),
    };

    if (!isMockEnabled()) {
      try {
        const response = await apiClient.post('/purchases', payload);
        const created = extractData(response);
        return normalizePurchase(created);
      } catch (err) {
        console.warn('POST /purchases failed:', err);
        throw err;
      }
    }

    await new Promise((r) => setTimeout(r, 300));
    const products = mockDb.getProducts();
    const suppliers = mockDb.getSuppliers();
    const transactions = mockDb.getTransactions();

    const supplier = suppliers.find((s) => String(s.id) === String(purchaseData.supplierId));
    const supplierName = supplier ? supplier.supplierName || supplier.name : 'Wholesaler Depot';

    let totalAmount = 0;
    const validatedItems = payload.items.map((item, idx) => {
      const product = products.find((p) => String(p.id) === String(item.productId));
      const prevStock = product ? product.currentStock : 0;
      if (product) {
        product.currentStock += item.quantity;
        if (item.unitCost > 0) {
          product.purchasePrice = item.unitCost;
        }

        // Ledger movement
        transactions.unshift({
          id: `tx-${Date.now()}-${idx}`,
          productId: product.id,
          productName: product.name,
          sku: product.sku,
          transactionType: TransactionType.Purchase,
          transactionTypeName: 'Purchase',
          quantity: item.quantity,
          previousStock: prevStock,
          newStock: product.currentStock,
          referenceId: purchaseData.invoiceNumber || `BILL-${Date.now()}`,
          notes: `Purchase from ${supplierName}`,
          createdAt: new Date().toISOString(),
        });
      }

      const subTotal = item.quantity * item.unitCost;
      totalAmount += subTotal;

      return {
        id: `pitem-${Date.now()}-${idx}`,
        productId: item.productId,
        productName: product ? product.name : `Product ${item.productId}`,
        sku: product ? product.sku : '',
        quantity: item.quantity,
        unitCost: item.unitCost,
        subTotal,
      };
    });

    const newPurchase = {
      id: `purch-${Date.now()}`,
      invoiceNumber: purchaseData.invoiceNumber || `BILL-${new Date().getFullYear()}-${String(Date.now()).slice(-4)}`,
      supplierId: purchaseData.supplierId,
      supplierName,
      purchaseDate: payload.purchaseDate,
      notes: payload.notes,
      totalAmount,
      items: validatedItems,
    };

    const purchases = mockDb.getPurchases();
    purchases.unshift(newPurchase);

    mockDb.savePurchases(purchases);
    mockDb.saveProducts(products);
    mockDb.saveTransactions(transactions);

    return newPurchase;
  },

  async getPurchasesSummary() {
    if (!isMockEnabled()) {
      try {
        const response = await apiClient.get('/purchases/summary');
        const data = extractData(response);
        if (data) return data;
      } catch (err) {
        console.warn('GET /purchases/summary failed:', err);
      }
    }
    const purchases = mockDb.getPurchases();
    const totalSpent = purchases.reduce((sum, p) => sum + (p.totalAmount || 0), 0);
    return {
      totalPurchasesCount: purchases.length,
      totalPurchasesAmount: totalSpent,
    };
  },
};

function normalizePurchase(p) {
  if (!p) return p;
  return {
    ...p,
    purchaseDate: p.purchaseDate || p.date,
    date: p.purchaseDate || p.date,
    totalAmount: p.totalAmount || 0,
    items: (p.items || []).map((i) => ({
      ...i,
      subtotal: i.subTotal ?? i.subtotal ?? (i.quantity * i.unitCost),
    })),
  };
}

export default purchaseService;
