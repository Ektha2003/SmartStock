import React from 'react';
import { Inbox } from 'lucide-react';
import { cn } from '../../utils/cn';

export function EmptyState({
  title = 'No records found',
  description = 'Get started by creating your first entry.',
  actionLabel,
  onAction,
  icon: Icon = Inbox,
  className,
}) {
  return (
    <div className={cn('flex flex-col items-center justify-center text-center p-6', className)}>
      <div className="w-12 h-12 rounded-full bg-slate-100 flex items-center justify-center text-slate-400 mb-3">
        <Icon className="w-6 h-6" />
      </div>
      <h4 className="text-base font-semibold text-slate-900">{title}</h4>
      <p className="text-sm text-slate-500 max-w-sm mt-1">{description}</p>
      {actionLabel && onAction && (
        <button
          onClick={onAction}
          className="mt-4 px-4 py-2 bg-indigo-600 text-white text-sm font-medium rounded-lg hover:bg-indigo-700 transition shadow-sm"
        >
          {actionLabel}
        </button>
      )}
    </div>
  );
}

export default EmptyState;

