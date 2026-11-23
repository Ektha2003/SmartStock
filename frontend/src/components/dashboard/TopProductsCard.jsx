import React from 'react';
import { formatCurrency } from '../../utils/formatters';
import { TrendingUp, Package } from 'lucide-react';
import { Link } from 'react-router-dom';

export function TopProductsCard({ products = [], isLoading = false }) {
  if (isLoading) {
    return (
      <div className="bg-white p-5 rounded-xl border border-slate-200/80 shadow-sm h-80 flex items-center justify-center">
        <div className="w-8 h-8 border-3 border-indigo-600 border-t-transparent rounded-full animate-spin" />
      </div>
    );
  }

  return (
    <div className="bg-white p-5 rounded-xl border border-slate-200/80 shadow-sm flex flex-col justify-between">
      <div>
        <div className="flex items-center justify-between mb-4">
          <div className="flex items-center gap-2">
            <div className="p-2 bg-indigo-50 text-indigo-700 rounded-lg">
              <TrendingUp className="w-4 h-4" />
            </div>
            <div>
              <h3 className="text-base font-bold text-slate-900">Top-Selling Products</h3>
              <p className="text-xs text-slate-500">Highest volume items this week</p>
            </div>
          </div>
          <Link to="/products" className="text-xs font-semibold text-indigo-600 hover:text-indigo-800">
            View All
          </Link>
        </div>

        <div className="space-y-3.5">
          {products.slice(0, 5).map((item, idx) => (
            <div key={item.id || idx} className="flex items-center justify-between text-sm">
              <div className="flex items-center gap-3 overflow-hidden">
                <span className="w-5 text-xs font-bold text-slate-400">#{idx + 1}</span>
                <div className="truncate">
                  <p className="font-medium text-slate-900 truncate max-w-[190px]">{item.name}</p>
                  <p className="text-[11px] text-slate-500">{item.quantitySold} units sold</p>
                </div>
              </div>
              <div className="text-right flex-shrink-0">
                <span className="font-bold text-slate-900">{formatCurrency(item.revenue)}</span>
              </div>
            </div>
          ))}
        </div>
      </div>

      <div className="mt-4 pt-3 border-t border-slate-100 flex items-center justify-between text-xs text-slate-500">
        <span>Contributing to top 64% basket revenue</span>
        <Package className="w-4 h-4 text-slate-400" />
      </div>
    </div>
  );
}

export default TopProductsCard;

