import React, { useState, useMemo } from 'react';
import { createPortal } from 'react-dom';
import { useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import {
  Sparkles,
  AlertTriangle,
  PackageX,
  TrendingUp,
  Truck,
  ArrowRight,
  Info,
  Calendar,
  IndianRupee,
} from 'lucide-react';
import forecastService from '../../services/forecastService';
import PageHeader from '../../components/common/PageHeader';
import StatCard from '../../components/common/StatCard';
import StatusBadge from '../../components/common/StatusBadge';
import { formatCurrency } from '../../utils/formatters';
import { toast } from 'sonner';

export function InsightsPage() {
  const navigate = useNavigate();

  const [selectedProduct, setSelectedProduct] = useState(null);

  const { data: forecastData, isLoading } = useQuery({
    queryKey: ['forecast'],
    queryFn: () => forecastService.getForecast(),
  });

  const { data: recommendationsData = [] } = useQuery({
    queryKey: ['recommendations', 'purchases'],
    queryFn: () => forecastService.getPurchaseRecommendations(),
  });

  const recommendations = useMemo(() => {
    if (Array.isArray(recommendationsData)) return recommendationsData;
    if (recommendationsData?.items && Array.isArray(recommendationsData.items)) return recommendationsData.items;
    return [];
  }, [recommendationsData]);

  const summary = forecastData?.summary || {
    productsAtRisk: 0,
    criticalProducts: 0,
    estimatedStockCoverageDays: 0,
    suggestedReorderValue: 0,
  };

  const forecasts = forecastData?.forecasts || [];

  const handleCreatePurchase = (prefillItems) => {
    if (!prefillItems || prefillItems.length === 0) {
      toast.info('No purchase items recommended currently.');
      return;
    }
    // Navigate to purchase form and preselect recommended items
    navigate('/purchases/new', { state: { prefill: prefillItems } });
  };

  return (
    <div className="space-y-6">
      <PageHeader
        title="AI Inventory Insights & Demand Forecasting"
        subtitle="Predict stockout dates, daily sales burn rate, and smart restocking orders."
        breadcrumbs={[{ label: 'AI Insights' }]}
        action={
          <button
            onClick={() => handleCreatePurchase(recommendations)}
            disabled={recommendations.length === 0}
            className="inline-flex items-center gap-2 px-4 py-2 bg-gradient-to-r from-indigo-600 to-indigo-700 hover:from-indigo-700 hover:to-indigo-800 text-white text-sm font-bold rounded-lg shadow-md transition disabled:opacity-50"
          >
            <Truck className="w-4 h-4" />
            <span>Create Purchase from Recommendations ({recommendations.length})</span>
          </button>
        }
      />

      {/* Top AI Summary KPI cards */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <StatCard
          title="Products at Stockout Risk"
          value={isLoading ? '...' : summary.productsAtRisk}
          subtitle="Stock coverage under 5 days"
          icon={AlertTriangle}
          color="amber"
        />
        <StatCard
          title="Critical / Depleted Items"
          value={isLoading ? '...' : summary.criticalProducts}
          subtitle="Zero or critically low units"
          icon={PackageX}
          color="red"
        />
        <StatCard
          title="Store Stock Coverage"
          value={isLoading ? '...' : `~${summary.estimatedStockCoverageDays} days`}
          subtitle="Average catalog runway"
          icon={TrendingUp}
          color="blue"
        />
        <StatCard
          title="Recommended Reorder Value"
          value={isLoading ? '...' : formatCurrency(summary.suggestedReorderValue)}
          subtitle="To maintain 14-day safety stock"
          icon={IndianRupee}
          color="indigo"
        />
      </div>

      {/* Recommendations Banner */}
      <div className="bg-gradient-to-r from-indigo-900 to-slate-900 text-white p-5 rounded-2xl shadow-md border border-indigo-800 flex flex-col md:flex-row items-center justify-between gap-4">
        <div className="flex items-start gap-3.5">
          <div className="p-3 bg-indigo-500/20 text-indigo-300 rounded-xl">
            <Sparkles className="w-6 h-6 text-amber-300" />
          </div>
          <div>
            <h3 className="text-base font-bold text-white">Smart Restock Order Optimizer</h3>
            <p className="text-xs text-slate-300 mt-1 max-w-2xl leading-relaxed">
              SmartStock’s AI model analyzed sales velocity and lead times. We found{' '}
              <span className="font-bold text-amber-300">{recommendations.length} SKUs</span> needing
              supplier replenishment. Click &quot;Create Purchase&quot; to review, customize quantities, and confirm the wholesale bill.
            </p>
          </div>
        </div>

        <button
          onClick={() => handleCreatePurchase(recommendations)}
          disabled={recommendations.length === 0}
          className="whitespace-nowrap px-4 py-2 bg-indigo-500 hover:bg-indigo-600 text-white font-semibold text-xs rounded-lg shadow-sm transition flex items-center gap-1.5"
        >
          <span>Pre-fill Wholesale Bill</span>
          <ArrowRight className="w-3.5 h-3.5" />
        </button>
      </div>

      {/* Main Forecasting Table */}
      <div className="bg-white rounded-xl border border-slate-200 overflow-hidden shadow-xs">
        <div className="p-4 border-b border-slate-200 bg-slate-50/60 flex items-center justify-between">
          <div>
            <h3 className="font-bold text-sm text-slate-900">Product Runway & Demand Forecasts</h3>
            <p className="text-xs text-slate-500">Calculated on 14-day rolling demand velocity</p>
          </div>
          <span className="text-xs font-semibold text-slate-500">
            {forecasts.length} Products Analyzed
          </span>
        </div>

        <div className="overflow-x-auto">
          <table className="w-full text-left border-collapse text-xs">
            <thead>
              <tr className="bg-slate-50/80 border-b border-slate-200 text-slate-600 uppercase tracking-wider font-semibold">
                <th className="py-3 px-4">Product</th>
                <th className="py-3 px-4 text-center">Stock on Hand</th>
                <th className="py-3 px-4 text-center">Avg Daily Sales</th>
                <th className="py-3 px-4 text-center">Days Remaining</th>
                <th className="py-3 px-4 text-center">Suggested Reorder</th>
                <th className="py-3 px-4 text-right">Est. Cost</th>
                <th className="py-3 px-4 text-center">Stockout Risk</th>
                <th className="py-3 px-4 text-right">Action</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {isLoading ? (
                <tr>
                  <td colSpan={8} className="py-12 text-center text-slate-400">
                    Calculating forecast models...
                  </td>
                </tr>
              ) : (
                forecasts.map((row) => (
                  <tr key={row.productId} className="hover:bg-slate-50/70 transition">
                    <td className="py-3 px-4">
                      <p className="font-semibold text-slate-900">{row.productName}</p>
                      <span className="text-[10px] text-slate-400 font-mono">{row.sku}</span>
                    </td>
                    <td className="py-3 px-4 text-center">
                      <span
                        className={`font-bold ${
                          row.currentStock === 0
                            ? 'text-rose-600'
                            : row.currentStock <= (row.minimumStock ?? row.minStock)
                            ? 'text-amber-600'
                            : 'text-slate-800'
                        }`}
                      >
                        {row.currentStock} {row.unitName || ''}
                      </span>
                    </td>
                    <td className="py-3 px-4 text-center font-medium text-slate-700">
                      {row.avgDailySales || row.averageDailySales || 2.0} {row.unitName || ''}/day
                    </td>
                    <td className="py-3 px-4 text-center">
                      <span
                        className={`font-bold text-xs px-2 py-0.5 rounded ${
                          row.daysRemaining === 'N/A' || row.daysRemaining <= 1
                            ? 'bg-rose-50 text-rose-700'
                            : row.daysRemaining <= 4
                            ? 'bg-amber-50 text-amber-700'
                            : 'bg-emerald-50 text-emerald-700'
                        }`}
                      >
                        {row.daysRemaining === 'N/A' ? '0 days' : `${row.daysRemaining} days`}
                      </span>
                    </td>
                    <td className="py-3 px-4 text-center">
                      {(row.suggestedOrderQuantity || row.suggestedQuantity) > 0 ? (
                        <span className="font-bold text-indigo-700 bg-indigo-50 px-2 py-0.5 rounded">
                          +{row.suggestedOrderQuantity || row.suggestedQuantity} {row.unitName || ''}
                        </span>
                      ) : (
                        <span className="text-slate-400">Optimal</span>
                      )}
                    </td>
                    <td className="py-3 px-4 text-right font-semibold text-slate-800">
                      {(row.suggestedOrderQuantity || row.suggestedQuantity) > 0 ? formatCurrency(row.estimatedPurchaseCost) : '-'}
                    </td>
                    <td className="py-3 px-4 text-center">
                      <StatusBadge status={row.riskLevelName || row.riskLevel} size="sm" />
                    </td>
                    <td className="py-3 px-4 text-right">
                      <button
                        onClick={() => setSelectedProduct(row)}
                        className="inline-flex items-center gap-1 text-indigo-600 hover:text-indigo-800 font-semibold p-1"
                      >
                        <Info className="w-3.5 h-3.5" />
                        <span>Insight</span>
                      </button>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* Forecast Detail Modal */}
      {selectedProduct && createPortal(
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
          <div
            onClick={() => setSelectedProduct(null)}
            className="fixed -inset-10 bg-slate-900/50 backdrop-blur-sm"
          />
          <div className="bg-white rounded-2xl max-w-lg w-full p-6 shadow-xl border border-slate-100 relative z-10 animate-in fade-in">
            <h3 className="text-base font-bold text-slate-900 mb-1">{selectedProduct.productName}</h3>
            <p className="text-xs text-slate-500 mb-4">Detailed AI Demand & Runway Breakdown</p>

            <div className="space-y-3 text-xs">
              <div className="p-3 bg-slate-50 rounded-lg space-y-1">
                <div className="flex justify-between">
                  <span className="text-slate-500">Current Shelf Stock:</span>
                  <span className="font-bold text-slate-900">{selectedProduct.currentStock} {selectedProduct.unit}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-slate-500">Average Daily Customer Demand:</span>
                  <span className="font-bold text-slate-900">{selectedProduct.avgDailySales} units/day</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-slate-500">Estimated Days Remaining:</span>
                  <span className="font-bold text-rose-600">{selectedProduct.daysRemaining} days</span>
                </div>
              </div>

              <div className="p-3 bg-indigo-50/70 border border-indigo-100 rounded-lg">
                <p className="font-bold text-indigo-900 mb-1 flex items-center gap-1">
                  <Sparkles className="w-3.5 h-3.5 text-indigo-700" />
                  AI Reorder Recommendation
                </p>
                <p className="text-slate-700 leading-relaxed">
                  To safeguard against stockouts over the next 14-day Kirana cycle, order{' '}
                  <span className="font-bold text-indigo-900">+{selectedProduct.suggestedQuantity} {selectedProduct.unit}</span>{' '}
                  at estimated cost of <span className="font-bold text-indigo-900">{formatCurrency(selectedProduct.estimatedPurchaseCost)}</span>.
                </p>
              </div>
            </div>

            <div className="mt-5 pt-3 border-t border-slate-100 flex items-center justify-end gap-2">
              <button
                type="button"
                onClick={() => setSelectedProduct(null)}
                className="px-4 py-2 text-xs font-semibold text-slate-600 bg-white border border-slate-200 rounded-lg hover:bg-slate-50"
              >
                Close
              </button>
              <button
                type="button"
                onClick={() => {
                  setSelectedProduct(null);
                  handleCreatePurchase([
                    {
                      productId: selectedProduct.productId,
                      productName: selectedProduct.productName,
                      sku: selectedProduct.sku,
                      suggestedQuantity: selectedProduct.suggestedQuantity,
                      unitCost: selectedProduct.purchasePrice,
                      unit: selectedProduct.unit,
                    },
                  ]);
                }}
                className="px-4 py-2 text-xs font-semibold text-white bg-indigo-600 hover:bg-indigo-700 rounded-lg shadow-sm"
              >
                Create Single Purchase Bill
              </button>
            </div>
          </div>
        </div>,
        document.body
      )}
    </div>
  );
}

export default InsightsPage;

