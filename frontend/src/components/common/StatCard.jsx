import React from 'react';
import { cn } from '../../utils/cn';

export function StatCard({ title, value, subtitle, icon: Icon, trend, trendValue, color = 'indigo', className }) {
  const colorStyles = {
    indigo: {
      bg: 'bg-indigo-50 text-indigo-700',
      border: 'border-indigo-100',
    },
    blue: {
      bg: 'bg-blue-50 text-blue-700',
      border: 'border-blue-100',
    },
    green: {
      bg: 'bg-emerald-50 text-emerald-700',
      border: 'border-emerald-100',
    },
    amber: {
      bg: 'bg-amber-50 text-amber-700',
      border: 'border-amber-100',
    },
    red: {
      bg: 'bg-red-50 text-red-700',
      border: 'border-red-100',
    },
  };

  const selectedColor = colorStyles[color] || colorStyles.indigo;

  return (
    <div className={cn('bg-white rounded-xl border border-slate-200/80 p-5 shadow-sm transition hover:shadow-md', className)}>
      <div className="flex items-center justify-between">
        <p className="text-sm font-medium text-slate-500">{title}</p>
        {Icon && (
          <div className={cn('p-2.5 rounded-lg flex items-center justify-center', selectedColor.bg)}>
            <Icon className="w-5 h-5" />
          </div>
        )}
      </div>
      <div className="mt-3 flex items-baseline gap-2">
        <h3 className="text-2xl font-bold tracking-tight text-slate-900">{value}</h3>
      </div>
      {(subtitle || trendValue) && (
        <div className="mt-2 flex items-center text-xs text-slate-500 gap-1.5">
          {trendValue && (
            <span
              className={cn(
                'inline-flex items-center font-semibold px-1.5 py-0.5 rounded text-[11px]',
                trend === 'up' ? 'bg-emerald-50 text-emerald-700' : 'bg-rose-50 text-rose-700'
              )}
            >
              {trend === 'up' ? '↑' : '↓'} {trendValue}
            </span>
          )}
          {subtitle && <span>{subtitle}</span>}
        </div>
      )}
    </div>
  );
}

export default StatCard;

