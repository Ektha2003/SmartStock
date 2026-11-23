import React from 'react';
import { AlertCircle, RefreshCw } from 'lucide-react';
import { cn } from '../../utils/cn';

export function ErrorState({
  title = 'Something went wrong',
  message = 'Failed to load data. Please check your connection or try again.',
  onRetry,
  className,
}) {
  return (
    <div className={cn('bg-white border border-rose-100 rounded-xl p-8 text-center flex flex-col items-center justify-center min-h-[220px]', className)}>
      <div className="w-12 h-12 rounded-full bg-rose-50 flex items-center justify-center text-rose-600 mb-3">
        <AlertCircle className="w-6 h-6" />
      </div>
      <h4 className="text-base font-semibold text-slate-900">{title}</h4>
      <p className="text-sm text-slate-500 max-w-sm mt-1">{message}</p>
      {onRetry && (
        <button
          onClick={onRetry}
          className="mt-4 inline-flex items-center gap-2 px-4 py-2 border border-slate-200 text-slate-700 bg-white rounded-lg hover:bg-slate-50 text-sm font-medium transition shadow-sm"
        >
          <RefreshCw className="w-4 h-4" /> Try Again
        </button>
      )}
    </div>
  );
}

export default ErrorState;

