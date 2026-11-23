import React from 'react';
import { cn } from '../../utils/cn';

export function StatusBadge({ status, size = 'md', className }) {
  const normalized = String(status || '').toLowerCase();

  let styles = 'bg-slate-100 text-slate-700 border-slate-200';

  if (['active', 'in stock', 'instock', 'healthy', 'paid', 'completed'].includes(normalized)) {
    styles = 'bg-emerald-50 text-emerald-700 border-emerald-200';
  } else if (['low stock', 'lowstock', 'medium', 'warning', 'pending'].includes(normalized)) {
    styles = 'bg-amber-50 text-amber-700 border-amber-200';
  } else if (['out of stock', 'outofstock', 'critical', 'high', 'danger', 'inactive', 'failed', 'cancelled'].includes(normalized)) {
    styles = 'bg-rose-50 text-rose-700 border-rose-200';
  }

  const sizes = {
    sm: 'text-[11px] px-2 py-0.5 font-medium',
    md: 'text-xs px-2.5 py-1 font-semibold',
  };

  return (
    <span
      className={cn(
        'inline-flex items-center rounded-full border',
        sizes[size] || sizes.md,
        styles,
        className
      )}
    >
      <span className="w-1.5 h-1.5 rounded-full bg-current mr-1.5 opacity-80" />
      {status}
    </span>
  );
}

export default StatusBadge;

