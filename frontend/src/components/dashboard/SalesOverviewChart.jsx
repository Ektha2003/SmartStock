import React from 'react';
import {
  ResponsiveContainer,
  AreaChart,
  Area,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
} from 'recharts';
import { formatCurrency } from '../../utils/formatters';

export function SalesOverviewChart({ data = [], isLoading = false }) {
  if (isLoading) {
    return (
      <div className="bg-white p-5 rounded-xl border border-slate-200/80 shadow-sm h-80 flex items-center justify-center">
        <div className="w-8 h-8 border-3 border-indigo-600 border-t-transparent rounded-full animate-spin" />
      </div>
    );
  }

  const CustomTooltip = ({ active, payload, label }) => {
    if (active && payload && payload.length) {
      return (
        <div className="bg-slate-900 text-white p-3 rounded-lg shadow-lg text-xs space-y-1">
          <p className="font-semibold">{label}</p>
          <p className="text-emerald-400 font-bold">
            Revenue: {formatCurrency(payload[0].value)}
          </p>
          {payload[0].payload.orders && (
            <p className="text-slate-300">
              Orders: {payload[0].payload.orders} bills
            </p>
          )}
        </div>
      );
    }
    return null;
  };

  return (
    <div className="bg-white p-5 rounded-xl border border-slate-200/80 shadow-sm">
      <div className="flex items-center justify-between mb-4">
        <div>
          <h3 className="text-base font-bold text-slate-900">Weekly Sales Velocity</h3>
          <p className="text-xs text-slate-500">Daily store revenue performance</p>
        </div>
        <span className="text-xs font-semibold px-2.5 py-1 bg-emerald-50 text-emerald-700 rounded-full border border-emerald-200">
          +18.4% this week
        </span>
      </div>
      <div className="h-64 w-full">
        <ResponsiveContainer width="100%" height="100%">
          <AreaChart data={data} margin={{ top: 10, right: 10, left: -15, bottom: 0 }}>
            <defs>
              <linearGradient id="salesGrad" x1="0" y1="0" x2="0" y2="1">
                <stop offset="5%" stopColor="#4F46E5" stopOpacity={0.25} />
                <stop offset="95%" stopColor="#4F46E5" stopOpacity={0.0} />
              </linearGradient>
            </defs>
            <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="#F1F5F9" />
            <XAxis dataKey="day" stroke="#94A3B8" fontSize={11} tickLine={false} />
            <YAxis
              stroke="#94A3B8"
              fontSize={11}
              tickLine={false}
              tickFormatter={(v) => `₹${v >= 1000 ? `${v / 1000}k` : v}`}
            />
            <Tooltip content={<CustomTooltip />} />
            <Area
              type="monotone"
              dataKey="sales"
              stroke="#4338CA"
              strokeWidth={2.5}
              fillOpacity={1}
              fill="url(#salesGrad)"
            />
          </AreaChart>
        </ResponsiveContainer>
      </div>
    </div>
  );
}

export default SalesOverviewChart;

