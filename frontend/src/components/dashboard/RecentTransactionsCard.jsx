import React from 'react';
import { Link } from 'react-router-dom';
import { ArrowUpDown, ArrowDownLeft, ArrowUpRight, SlidersHorizontal } from 'lucide-react';
import { formatDateTime } from '../../utils/formatters';

export function RecentTransactionsCard({ transactions = [], isLoading = false }) {
  if (isLoading) {
    return (
      <div className="bg-white p-5 rounded-xl border border-slate-200/80 shadow-sm h-72 flex items-center justify-center">
        <div className="w-8 h-8 border-3 border-indigo-600 border-t-transparent rounded-full animate-spin" />
      </div>
    );
  }

  const getTypeIcon = (type) => {
    if (type === 'Sale') {
      return <ArrowDownLeft className="w-3.5 h-3.5 text-emerald-600" />;
    }
    if (type === 'Purchase') {
      return <ArrowUpRight className="w-3.5 h-3.5 text-blue-600" />;
    }
    return <SlidersHorizontal className="w-3.5 h-3.5 text-amber-600" />;
  };

  return (
    <div className="bg-white p-5 rounded-xl border border-slate-200/80 shadow-sm">
      <div className="flex items-center justify-between mb-4">
        <div className="flex items-center gap-2">
          <div className="p-2 bg-slate-100 text-slate-700 rounded-lg">
            <ArrowUpDown className="w-4 h-4" />
          </div>
          <div>
            <h3 className="text-base font-bold text-slate-900">Recent Stock Movements</h3>
            <p className="text-xs text-slate-500">Live ledger of sales and purchases</p>
          </div>
        </div>
        <Link to="/inventory/transactions" className="text-xs font-semibold text-indigo-600 hover:text-indigo-800">
          Full Ledger
        </Link>
      </div>

      <div className="divide-y divide-slate-100">
        {transactions.slice(0, 5).map((t) => (
          <div key={t.id} className="py-2.5 flex items-center justify-between text-xs">
            <div className="flex items-center gap-2.5 overflow-hidden">
              <div className="p-1.5 rounded bg-slate-100">{getTypeIcon(t.type)}</div>
              <div className="truncate">
                <p className="font-semibold text-slate-900 truncate max-w-[200px]">{t.productName}</p>
                <p className="text-slate-400">{t.reference} • {formatDateTime(t.date)}</p>
              </div>
            </div>
            <div className="text-right flex-shrink-0">
              <span
                className={`font-bold ${
                  t.quantity > 0 ? 'text-emerald-600' : 'text-slate-800'
                }`}
              >
                {t.quantity > 0 ? `+${t.quantity}` : t.quantity}
              </span>
              <p className="text-[10px] text-slate-400">Balance: {t.newStock}</p>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}

export default RecentTransactionsCard;

