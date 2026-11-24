import React from 'react';
import { ChevronLeft, ChevronRight } from 'lucide-react';
import { cn } from '../../utils/cn';

const range = (start, end) => Array.from({ length: end - start + 1 }, (_, i) => start + i);

/**
 * Builds a compact page list such as [1, 'ellipsis-start', 6, 7, 8, 'ellipsis-end', 20].
 * Once there are more pages than fit, the list always has the same number of slots,
 * so the control keeps a stable width while moving between pages.
 */
export function getPageItems(currentPage, totalPages, siblingCount = 1) {
  const totalSlots = siblingCount * 2 + 5; // first + last + current + 2 ellipses
  if (totalPages <= totalSlots) return range(1, totalPages);

  const leftSibling = Math.max(currentPage - siblingCount, 1);
  const rightSibling = Math.min(currentPage + siblingCount, totalPages);
  // Only collapse when the ellipsis would hide at least two pages.
  const showLeftEllipsis = leftSibling > 3;
  const showRightEllipsis = rightSibling < totalPages - 2;
  const edgeCount = 3 + siblingCount * 2;

  if (!showLeftEllipsis) {
    return [...range(1, edgeCount), 'ellipsis-end', totalPages];
  }
  if (!showRightEllipsis) {
    return [1, 'ellipsis-start', ...range(totalPages - edgeCount + 1, totalPages)];
  }
  return [1, 'ellipsis-start', ...range(leftSibling, rightSibling), 'ellipsis-end', totalPages];
}

export function Pagination({
  currentPage = 1,
  totalPages = 1,
  onPageChange,
  totalCount,
  pageSize,
  className,
}) {
  if (totalPages <= 1) return null;

  const page = Math.min(Math.max(1, currentPage), totalPages);
  const items = getPageItems(page, totalPages);

  const goTo = (target) => {
    if (target < 1 || target > totalPages || target === page) return;
    onPageChange?.(target);
  };

  const hasCounts = Number.isFinite(totalCount) && Number.isFinite(pageSize) && totalCount > 0;
  const from = hasCounts ? (page - 1) * pageSize + 1 : 0;
  const to = hasCounts ? Math.min(page * pageSize, totalCount) : 0;

  const arrowClass =
    'inline-flex items-center justify-center w-8 h-8 rounded-lg border border-slate-200 text-slate-600 hover:bg-slate-50 disabled:opacity-40 disabled:cursor-not-allowed disabled:hover:bg-transparent transition';

  return (
    <nav
      aria-label="Pagination"
      className={cn(
        'flex flex-col sm:flex-row items-center justify-between gap-3 px-4 py-3 bg-white border-t border-slate-200 text-sm',
        className
      )}
    >
      <p className="text-slate-500 text-xs">
        {hasCounts ? (
          <>
            Showing <span className="font-semibold text-slate-900">{from}</span>–
            <span className="font-semibold text-slate-900">{to}</span> of{' '}
            <span className="font-semibold text-slate-900">{totalCount}</span>
          </>
        ) : (
          <>
            Page <span className="font-semibold text-slate-900">{page}</span> of{' '}
            <span className="font-semibold text-slate-900">{totalPages}</span>
          </>
        )}
      </p>

      <div className="flex items-center gap-1">
        <button
          type="button"
          onClick={() => goTo(page - 1)}
          disabled={page <= 1}
          aria-label="Previous page"
          className={arrowClass}
        >
          <ChevronLeft className="w-4 h-4" />
        </button>

        {items.map((item) =>
          typeof item === 'string' ? (
            <span
              key={item}
              aria-hidden="true"
              className="w-8 h-8 inline-flex items-center justify-center text-xs text-slate-400 select-none"
            >
              …
            </span>
          ) : (
            <button
              type="button"
              key={item}
              onClick={() => goTo(item)}
              aria-label={`Page ${item}`}
              aria-current={item === page ? 'page' : undefined}
              className={cn(
                'min-w-8 h-8 px-1 rounded-lg text-xs font-medium tabular-nums transition',
                item === page
                  ? 'bg-indigo-600 text-white shadow-sm cursor-default'
                  : 'text-slate-600 hover:bg-slate-100'
              )}
            >
              {item}
            </button>
          )
        )}

        <button
          type="button"
          onClick={() => goTo(page + 1)}
          disabled={page >= totalPages}
          aria-label="Next page"
          className={arrowClass}
        >
          <ChevronRight className="w-4 h-4" />
        </button>
      </div>
    </nav>
  );
}

export default Pagination;
