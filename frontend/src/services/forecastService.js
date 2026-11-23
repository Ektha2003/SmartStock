import apiClient, { extractData, isMockEnabled } from './api';
import { mockDb } from './mockData';
import { StockRiskLevel, StockRiskLevelName } from '../utils/constants';

export const forecastService = {
  async getForecast() {
    if (!isMockEnabled()) {
      try {
        const response = await apiClient.get('/forecast');
        const data = extractData(response);
        if (Array.isArray(data)) {
          return buildForecastSummary(data.map(normalizeForecastItem));
        }
        if (data?.forecasts) {
          return data;
        }
      } catch (err) {
        console.warn('GET /forecast failed, calculating from dynamic store:', err);
      }
    }

    await new Promise((r) => setTimeout(r, 200));
    const products = mockDb.getProducts();

    const items = products.map((product) => {
      const avgDaily = Number(product.avgDailySales) || 2.0;
      const stock = Number(product.currentStock) || 0;
      const daysRemaining = avgDaily > 0 ? (stock / avgDaily) : 999;
      const targetStock = Math.ceil(avgDaily * 14); // 14 days safety stock
      const suggestedQty = Math.max(0, targetStock - stock);

      let riskLevel = StockRiskLevel.Healthy;
      let riskLevelName = StockRiskLevelName[StockRiskLevel.Healthy];

      if (stock === 0) {
        riskLevel = StockRiskLevel.Critical;
        riskLevelName = 'Critical';
      } else if (daysRemaining <= 2.5 || stock <= product.minimumStock * 0.5) {
        riskLevel = StockRiskLevel.Critical;
        riskLevelName = 'Critical';
      } else if (daysRemaining <= 5 || stock <= product.minimumStock) {
        riskLevel = StockRiskLevel.High;
        riskLevelName = 'High';
      } else if (daysRemaining <= 8) {
        riskLevel = StockRiskLevel.Medium;
        riskLevelName = 'Medium';
      }

      return {
        productId: product.id,
        productName: product.name,
        sku: product.sku,
        categoryName: product.categoryName || 'General',
        currentStock: stock,
        minimumStock: product.minimumStock,
        unit: product.unit,
        unitName: product.unitName || 'Piece',
        purchasePrice: product.purchasePrice,
        totalUnitsSoldLast7Days: Number((avgDaily * 7).toFixed(1)),
        averageDailySales: Number(avgDaily.toFixed(1)),
        avgDailySales: Number(avgDaily.toFixed(1)),
        estimatedDaysRemaining: daysRemaining === 999 ? 99 : Number(daysRemaining.toFixed(1)),
        daysRemaining: daysRemaining === 999 ? 'N/A' : Number(daysRemaining.toFixed(1)),
        targetStock,
        suggestedOrderQuantity: suggestedQty,
        suggestedQuantity: suggestedQty,
        estimatedPurchaseCost: suggestedQty * product.purchasePrice,
        riskLevel,
        riskLevelName,
        explanation: `${riskLevelName}: Stock will cover ~${daysRemaining.toFixed(1)} days based on current burn rate.`,
      };
    });

    return buildForecastSummary(items);
  },

  async getForecastById(productId) {
    if (!isMockEnabled()) {
      try {
        const response = await apiClient.get(`/forecast/${productId}`);
        const data = extractData(response);
        if (data) return normalizeForecastItem(data);
      } catch (err) {
        console.warn(`GET /forecast/${productId} failed:`, err);
      }
    }
    const all = await this.getForecast();
    return all.forecasts.find((f) => String(f.productId) === String(productId));
  },

  async getPurchaseRecommendations() {
    if (!isMockEnabled()) {
      try {
        const response = await apiClient.get('/recommendations/purchases');
        const data = extractData(response);
        if (Array.isArray(data)) {
          return data.map(normalizeRecommendation);
        }
      } catch (err) {
        console.warn('GET /recommendations/purchases failed, calculating dynamically:', err);
      }
    }

    await new Promise((r) => setTimeout(r, 150));
    const forecastData = await this.getForecast();
    const suppliers = mockDb.getSuppliers();
    const defaultSupplier = suppliers[0] || { id: 'sup-1', supplierName: 'Delhi Wholesale FMCG Traders' };

    return forecastData.forecasts
      .filter((item) => item.suggestedOrderQuantity > 0 && ['Critical', 'High', 'Medium'].includes(item.riskLevelName))
      .map((item) => ({
        productId: item.productId,
        productName: item.productName,
        sku: item.sku,
        unit: item.unit,
        unitName: item.unitName,
        currentStock: item.currentStock,
        averageDailySales: item.averageDailySales,
        suggestedOrderQuantity: item.suggestedOrderQuantity,
        suggestedQuantity: item.suggestedOrderQuantity,
        unitPurchasePrice: item.purchasePrice,
        unitCost: item.purchasePrice,
        estimatedPurchaseCost: item.estimatedPurchaseCost,
        riskLevelName: item.riskLevelName,
        preferredSupplierId: defaultSupplier.id,
        preferredSupplierName: defaultSupplier.supplierName || defaultSupplier.name,
        reason: item.explanation || `Buffer 14 days of demand: ${item.targetStock} units needed, ${item.currentStock} in stock.`,
      }));
  },
};

function normalizeForecastItem(item) {
  const suggested = item.suggestedOrderQuantity ?? item.suggestedQuantity ?? 0;
  const cost = item.estimatedPurchaseCost ?? (suggested * (item.purchasePrice || item.unitPurchasePrice || 0));
  const days = item.estimatedDaysRemaining ?? item.daysRemaining ?? 0;
  const avg = item.averageDailySales ?? item.avgDailySales ?? 2.0;

  return {
    ...item,
    suggestedOrderQuantity: suggested,
    suggestedQuantity: suggested,
    estimatedPurchaseCost: cost,
    averageDailySales: avg,
    avgDailySales: avg,
    estimatedDaysRemaining: days,
    daysRemaining: days,
    riskLevelName: item.riskLevelName || StockRiskLevelName[item.riskLevel] || 'Healthy',
  };
}

function normalizeRecommendation(item) {
  const suggested = item.suggestedOrderQuantity ?? item.suggestedQuantity ?? 0;
  const unitCost = item.unitPurchasePrice ?? item.unitCost ?? 0;
  const cost = item.estimatedPurchaseCost ?? (suggested * unitCost);

  return {
    ...item,
    suggestedOrderQuantity: suggested,
    suggestedQuantity: suggested,
    unitPurchasePrice: unitCost,
    unitCost,
    estimatedPurchaseCost: cost,
    riskLevelName: item.riskLevelName || 'High',
  };
}

function buildForecastSummary(forecasts) {
  const severityWeight = { Critical: 4, High: 3, Medium: 2, Healthy: 1 };
  forecasts.sort((a, b) => (severityWeight[b.riskLevelName] || 1) - (severityWeight[a.riskLevelName] || 1));

  const productsAtRisk = forecasts.filter((i) => i.riskLevelName === 'Critical' || i.riskLevelName === 'High').length;
  const criticalProducts = forecasts.filter((i) => i.riskLevelName === 'Critical').length;
  const suggestedReorderValue = forecasts.reduce((sum, i) => sum + (i.estimatedPurchaseCost || 0), 0);

  return {
    summary: {
      productsAtRisk,
      criticalProducts,
      estimatedStockCoverageDays: 9.4,
      suggestedReorderValue,
    },
    forecasts,
  };
}

export default forecastService;
