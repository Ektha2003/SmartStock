import React from 'react';
import { ChevronLeft, ChevronRight } from 'lucide-react';
import { cn } from '../../utils/cn';

export function Pagination({ currentPage = 1, totalPages = 1, onPageChange, className }) {
  if (totalPages <= 1) return null;

  return (
    <div className={cn('flex items-center justify-between px-4 py-3 bg-white border-t border-slate-200 text-sm', className)}>
      <p className="text-slate-500 text-xs">
        Page <span className="font-semibold text-slate-900">{currentPage}</span> of{' '}
        <span className="font-semibold text-slate-900">{totalPages}</span>
      </p>
      <div className="flex items-center gap-1">
        <button
          onClick={() => onPageChange(currentPage - 1)}
          disabled={currentPage <= 1}
          className="p-1.5 rounded-lg border border-slate-200 text-slate-600 hover:bg-slate-50 disabled:opacity-40 disabled:cursor-not-allowed transition"
        >
          <ChevronLeft className="w-4 h-4" />
        </button>
        {Array.from({ length: totalPages }, (_, i) => i + 1).map((page) => (
          <button
            key={page}
            onClick={() => onPageChange(page)}
            className={cn(
              'w-8 h-8 rounded-lg text-xs font-medium transition',
              currentPage === page
                ? 'bg-indigo-600 text-white'
                : 'text-slate-600 hover:bg-slate-100'
            )}
          >
            {page}
          </button>
        ))}
        <button
          onClick={() => onPageChange(currentPage + 1)}
          disabled={currentPage >= totalPages}
          className="p-1.5 rounded-lg border border-slate-200 text-slate-600 hover:bg-slate-50 disabled:opacity-40 disabled:cursor-not-allowed transition"
        >
          <ChevronRight className="w-4 h-4" />
        </button>
      </div>
    </div>
  );
}

export default Pagination;

