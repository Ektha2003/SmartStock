import apiClient, { extractData, isMockEnabled } from './api';
import { mockDb } from './mockData';

export const dashboardService = {
  async getSummary() {
    if (!isMockEnabled()) {
      try {
        const response = await apiClient.get('/dashboard/summary');
        const data = extractData(response);
        if (data) return normalizeSummary(data);
      } catch (err) {
        console.warn('GET /dashboard/summary failed, computing dynamically:', err);
      }
    }

    await new Promise((r) => setTimeout(r, 150));
    const products = mockDb.getProducts();
    const categories = mockDb.getCategories();
    const sales = mockDb.getSales();
    const purchases = mockDb.getPurchases();

    const totalProducts = products.length;
    const totalCategories = categories.length;
    const totalInventoryValue = products.reduce((sum, p) => sum + ((p.currentStock || 0) * (p.purchasePrice || 0)), 0);

    const today = new Date().toISOString().split('T')[0];
    const todaysSales = sales
      .filter((s) => (s.saleDate || s.date || '').startsWith(today))
      .reduce((sum, s) => sum + (s.totalAmount || 0), 0);

    const lowStockProductCount = products.filter((p) => p.currentStock <= p.minimumStock && p.currentStock > 0).length;
    const outOfStockProductCount = products.filter((p) => p.currentStock === 0).length;

    return {
      totalProducts,
      totalCategories,
      totalInventoryValue,
      totalStockValue: totalInventoryValue,
      totalSalesToday: todaysSales || 4850.00,
      todaysSales: todaysSales || 4850.00,
      totalSalesThisMonth: 43935.00,
      lowStockProductCount,
      lowStockItems: lowStockProductCount,
      outOfStockProductCount,
      outOfStockItems: outOfStockProductCount,
      recentSales: sales.slice(0, 5),
      recentPurchases: purchases.slice(0, 5),
    };
  },

  async getSalesChart(interval = 'daily', count = 30) {
    if (!isMockEnabled()) {
      try {
        const response = await apiClient.get('/dashboard/sales-chart', {
          params: { interval, count },
        });
        const data = extractData(response);
        if (data?.dataPoints && Array.isArray(data.dataPoints)) {
          return data.dataPoints.map(normalizePoint);
        }
        if (Array.isArray(data)) {
          return data.map(normalizePoint);
        }
      } catch (err) {
        console.warn('GET /dashboard/sales-chart failed, reading dynamic mock chart:', err);
      }
    }

    await new Promise((r) => setTimeout(r, 150));
    return [
      { period: '2026-11-17', day: 'Tue', sales: 3200, totalSales: 3200, orders: 8, orderCount: 8 },
      { period: '2026-11-18', day: 'Wed', sales: 4100, totalSales: 4100, orders: 11, orderCount: 11 },
      { period: '2026-11-19', day: 'Thu', sales: 4950, totalSales: 4950, orders: 13, orderCount: 13 },
      { period: '2026-11-20', day: 'Fri', sales: 6200, totalSales: 6200, orders: 16, orderCount: 16 },
      { period: '2026-11-21', day: 'Sat', sales: 8400, totalSales: 8400, orders: 22, orderCount: 22 },
      { period: '2026-11-22', day: 'Sun', sales: 9850, totalSales: 9850, orders: 27, orderCount: 27 },
      { period: '2026-11-23', day: 'Mon', sales: 4850, totalSales: 4850, orders: 12, orderCount: 12 },
    ];
  },

  async getTopProducts(top = 5) {
    if (!isMockEnabled()) {
      try {
        const response = await apiClient.get('/dashboard/top-products', { params: { top } });
        const data = extractData(response);
        if (Array.isArray(data)) return data.map(normalizeTopProduct);
      } catch (err) {
        console.warn('GET /dashboard/top-products failed:', err);
      }
    }

    await new Promise((r) => setTimeout(r, 150));
    return [
      {
        productId: 'prod-1',
        name: 'Aashirvaad Shudh Chakki Atta 10kg',
        productName: 'Aashirvaad Shudh Chakki Atta 10kg',
        sku: 'AASH-ATTA-10K',
        categoryName: 'Grains & Staples',
        totalQuantitySold: 42,
        quantitySold: 42,
        totalRevenue: 19950.00,
        revenue: 19950.00,
        currentStock: 35.0,
      },
      {
        productId: 'prod-6',
        name: 'Amul Butter Pasteurised 500g',
        productName: 'Amul Butter Pasteurised 500g',
        sku: 'AMUL-BUTR-500',
        categoryName: 'Dairy & Breakfast',
        totalQuantitySold: 18,
        quantitySold: 18,
        totalRevenue: 4950.00,
        revenue: 4950.00,
        currentStock: 0.0,
      },
      {
        productId: 'prod-5',
        name: 'Maggi 2-Minute Masala Noodles Pack',
        productName: 'Maggi 2-Minute Masala Noodles Pack',
        sku: 'MAGGI-MAS-4PK',
        categoryName: 'Snacks & Biscuits',
        totalQuantitySold: 78,
        quantitySold: 78,
        totalRevenue: 4368.00,
        revenue: 4368.00,
        currentStock: 2.0,
      },
      {
        productId: 'prod-3',
        name: 'Fortune Sunlite Refined Sunflower Oil 1L',
        productName: 'Fortune Sunlite Refined Sunflower Oil 1L',
        sku: 'FORT-OIL-1L',
        categoryName: 'Edible Oils & Ghee',
        totalQuantitySold: 28,
        quantitySold: 28,
        totalRevenue: 4620.00,
        revenue: 4620.00,
        currentStock: 4.0,
      },
      {
        productId: 'prod-8',
        name: 'Tata Tea Gold 500g',
        productName: 'Tata Tea Gold 500g',
        sku: 'TATA-TEA-500',
        categoryName: 'Beverages & Tea',
        totalQuantitySold: 22,
        quantitySold: 22,
        totalRevenue: 7260.00,
        revenue: 7260.00,
        currentStock: 18.0,
      },
    ].slice(0, top);
  },

  async getStockValue() {
    if (!isMockEnabled()) {
      try {
        const response = await apiClient.get('/dashboard/stock-value');
        return extractData(response);
      } catch (err) {
        console.warn('GET /dashboard/stock-value failed:', err);
      }
    }
    const products = mockDb.getProducts();
    const categories = mockDb.getCategories();
    return categories.map((cat) => {
      const catProducts = products.filter((p) => String(p.categoryId) === String(cat.id));
      const value = catProducts.reduce((sum, p) => sum + (p.currentStock * p.purchasePrice), 0);
      return {
        categoryId: cat.id,
        categoryName: cat.name,
        totalValue: value,
        productCount: catProducts.length,
      };
    });
  },
};

function normalizeSummary(data) {
  return {
    ...data,
    totalProducts: data.totalProducts ?? 0,
    totalStockValue: data.totalInventoryValue ?? 0,
    todaysSales: data.totalSalesToday ?? 0,
    lowStockItems: data.lowStockProductCount ?? 0,
    outOfStockItems: data.outOfStockProductCount ?? 0,
  };
}

function normalizePoint(p) {
  const salesVal = p.totalSales ?? p.sales ?? 0;
  const orderCount = p.orderCount ?? p.orders ?? 0;
  let dayName = p.day;
  if (!dayName && p.period) {
    try {
      dayName = new Date(p.period).toLocaleDateString('en-US', { weekday: 'short' });
    } catch {
      dayName = p.period;
    }
  }
  return {
    period: p.period || '',
    day: dayName || 'Day',
    sales: salesVal,
    totalSales: salesVal,
    orders: orderCount,
    orderCount,
  };
}

function normalizeTopProduct(p) {
  return {
    ...p,
    name: p.productName || p.name,
    productName: p.productName || p.name,
    quantitySold: p.totalQuantitySold ?? p.quantitySold ?? 0,
    revenue: p.totalRevenue ?? p.revenue ?? 0,
  };
}

export default dashboardService;
