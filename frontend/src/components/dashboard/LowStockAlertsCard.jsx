import React from 'react';
import { Link } from 'react-router-dom';
import { AlertTriangle, ArrowRight, ShieldCheck } from 'lucide-react';
import StatusBadge from '../common/StatusBadge';

export function LowStockAlertsCard({ products = [], isLoading = false }) {
  if (isLoading) {
    return (
      <div className="bg-white p-5 rounded-xl border border-slate-200/80 shadow-sm h-80 flex items-center justify-center">
        <div className="w-8 h-8 border-3 border-indigo-600 border-t-transparent rounded-full animate-spin" />
      </div>
    );
  }

  const alertItems = products.filter((p) => p.currentStock <= p.minStock);

  return (
    <div className="bg-white p-5 rounded-xl border border-slate-200/80 shadow-sm flex flex-col justify-between">
      <div>
        <div className="flex items-center justify-between mb-4">
          <div className="flex items-center gap-2">
            <div className="p-2 bg-rose-50 text-rose-600 rounded-lg">
              <AlertTriangle className="w-4 h-4" />
            </div>
            <div>
              <h3 className="text-base font-bold text-slate-900">Low-Stock Alerts</h3>
              <p className="text-xs text-slate-500">Items needing prompt restocking</p>
            </div>
          </div>
          <Link to="/notifications" className="text-xs font-semibold text-rose-600 hover:text-rose-800">
            View All ({alertItems.length})
          </Link>
        </div>

        {alertItems.length === 0 ? (
          <div className="py-8 flex flex-col items-center justify-center text-center text-slate-500">
            <ShieldCheck className="w-8 h-8 text-emerald-500 mb-2" />
            <p className="text-sm font-medium text-slate-800">Stock levels healthy!</p>
            <p className="text-xs text-slate-400">No items are below minimum threshold.</p>
          </div>
        ) : (
          <div className="space-y-3">
            {alertItems.slice(0, 4).map((p) => {
              const isOut = p.currentStock === 0;
              return (
                <div
                  key={p.id}
                  className="flex items-center justify-between p-2.5 rounded-lg bg-slate-50/80 border border-slate-100 hover:bg-slate-100/60 transition"
                >
                  <div className="truncate mr-2">
                    <p className="font-semibold text-sm text-slate-900 truncate max-w-[200px]">{p.name}</p>
                    <p className="text-[11px] text-slate-500">
                      Stock: <span className="font-bold text-slate-800">{p.currentStock} {p.unit}</span> (Min: {p.minStock})
                    </p>
                  </div>
                  <div className="flex items-center gap-2 flex-shrink-0">
                    <StatusBadge status={isOut ? 'Critical' : 'Low Stock'} size="sm" />
                    <Link
                      to={`/products/${p.id}`}
                      className="text-slate-400 hover:text-indigo-600 p-1"
                      title="View product"
                    >
                      <ArrowRight className="w-3.5 h-3.5" />
                    </Link>
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </div>

      <div className="mt-4 pt-3 border-t border-slate-100">
        <Link
          to="/insights"
          className="w-full py-2 bg-indigo-50 hover:bg-indigo-100 text-indigo-700 text-xs font-semibold rounded-lg transition flex items-center justify-center gap-1.5"
        >
          <span>Open AI Restock Recommendations</span>
          <ArrowRight className="w-3.5 h-3.5" />
        </Link>
      </div>
    </div>
  );
}

export default LowStockAlertsCard;

