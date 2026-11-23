import React from 'react';
import { cn } from '../../utils/cn';
import EmptyState from './EmptyState';
import LoadingSpinner from './LoadingSpinner';

export function DataTable({
  columns = [],
  data = [],
  isLoading = false,
  emptyTitle = 'No records found',
  emptyDescription = 'There is no data to display matching your criteria.',
  onRowClick,
  className,
}) {
  if (isLoading) {
    return (
      <div className="bg-white border border-slate-200 rounded-xl p-12 flex flex-col items-center justify-center min-h-[300px]">
        <LoadingSpinner size="lg" text="Fetching records..." />
      </div>
    );
  }

  if (!data || data.length === 0) {
    return (
      <div className="bg-white border border-slate-200 rounded-xl p-8 min-h-[260px] flex items-center justify-center">
        <EmptyState title={emptyTitle} description={emptyDescription} />
      </div>
    );
  }

  return (
    <div className={cn('bg-white border border-slate-200 rounded-xl overflow-hidden shadow-sm', className)}>
      <div className="overflow-x-auto">
        <table className="w-full text-left border-collapse text-sm">
          <thead>
            <tr className="bg-slate-50/80 border-b border-slate-200/90 text-slate-600 font-semibold text-xs tracking-wider uppercase">
              {columns.map((col, index) => (
                <th
                  key={col.key || index}
                  className={cn(
                    'py-3.5 px-4',
                    col.align === 'right' ? 'text-right' : col.align === 'center' ? 'text-center' : 'text-left',
                    col.headerClassName
                  )}
                >
                  {col.header}
                </th>
              ))}
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100">
            {data.map((row, rowIndex) => (
              <tr
                key={row.id || rowIndex}
                onClick={() => onRowClick && onRowClick(row)}
                className={cn(
                  'transition hover:bg-slate-50/70',
                  onRowClick && 'cursor-pointer'
                )}
              >
                {columns.map((col, colIndex) => (
                  <td
                    key={col.key || colIndex}
                    className={cn(
                      'py-3.5 px-4 text-slate-700 font-normal',
                      col.align === 'right' ? 'text-right' : col.align === 'center' ? 'text-center' : 'text-left',
                      col.cellClassName
                    )}
                  >
                    {col.render ? col.render(row, rowIndex) : row[col.accessor || col.key]}
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

export default DataTable;

