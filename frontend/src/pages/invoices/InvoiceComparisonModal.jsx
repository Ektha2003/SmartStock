import React from 'react';
import { useQuery } from '@tanstack/react-query';
import {
  X,
  TrendingUp,
  TrendingDown,
  Minus,
  AlertTriangle,
  ArrowRight,
  Loader2,
  FileSpreadsheet
} from 'lucide-react';
import supplierInvoiceService from '../../services/supplierInvoiceService';
import { formatCurrency, formatDate } from '../../utils/formatters';

export function InvoiceComparisonModal({ isOpen, onClose, invoiceId }) {
  const { data: comparison, isLoading, error } = useQuery({
    queryKey: ['invoice-comparison', invoiceId],
    queryFn: () => supplierInvoiceService.compareInvoice(invoiceId),
    enabled: !!invoiceId && isOpen,
  });

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 overflow-y-auto bg-slate-900/50 backdrop-blur-xs flex items-center justify-center p-4">
      <div className="bg-white rounded-2xl border border-slate-200 shadow-2xl w-full max-w-4xl max-h-[90vh] flex flex-col overflow-hidden">
        {/* Header */}
        <div className="px-6 py-4 border-b border-slate-100 flex items-center justify-between bg-slate-50/70">
          <div>
            <h3 className="font-bold text-slate-900 text-base flex items-center gap-2">
              <FileSpreadsheet className="w-5 h-5 text-indigo-600" />
              Supplier Invoice Comparison & Variance Analysis
            </h3>
            <p className="text-xs text-slate-500 mt-0.5">
              Side-by-side item rate & quantity changes against the previous purchase cycle
            </p>
          </div>
          <button
            onClick={onClose}
            className="p-1.5 text-slate-400 hover:text-slate-600 rounded-lg hover:bg-slate-100 transition"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Content */}
        <div className="p-6 overflow-y-auto space-y-6">
          {isLoading ? (
            <div className="py-16 flex flex-col items-center justify-center gap-3">
              <Loader2 className="w-8 h-8 animate-spin text-indigo-600" />
              <p className="text-sm text-slate-500">Comparing line items and purchase price variance...</p>
            </div>
          ) : error || !comparison ? (
            <div className="text-center py-12 text-slate-500">
              <p className="text-sm">Unable to generate comparison. Please try again.</p>
            </div>
          ) : (
            <>
              {/* Top Level Comparison Cards */}
              <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                <div className="bg-slate-50 p-4 rounded-xl border border-slate-200/80">
                  <p className="text-xs font-semibold text-slate-400 uppercase tracking-wider">Current Invoice</p>
                  <p className="text-base font-bold text-slate-900 mt-1">
                    #{comparison.currentInvoice?.invoiceNumber}
                  </p>
                  <p className="text-xs text-slate-500 mt-0.5">
                    {formatDate(comparison.currentInvoice?.invoiceDate)} • {formatCurrency(comparison.currentInvoice?.grandTotal)}
                  </p>
                </div>

                <div className="bg-slate-50 p-4 rounded-xl border border-slate-200/80">
                  <p className="text-xs font-semibold text-slate-400 uppercase tracking-wider">Previous Invoice</p>
                  {comparison.previousInvoice ? (
                    <>
                      <p className="text-base font-bold text-slate-900 mt-1">
                        #{comparison.previousInvoice.invoiceNumber}
                      </p>
                      <p className="text-xs text-slate-500 mt-0.5">
                        {formatDate(comparison.previousInvoice.invoiceDate)} • {formatCurrency(comparison.previousInvoice.grandTotal)}
                      </p>
                    </>
                  ) : (
                    <p className="text-sm text-slate-500 italic mt-2">No earlier invoice recorded for comparison.</p>
                  )}
                </div>

                <div className={`p-4 rounded-xl border ${
                  comparison.totalPriceVariance > 0
                    ? 'bg-rose-50/70 border-rose-200 text-rose-900'
                    : comparison.totalPriceVariance < 0
                    ? 'bg-emerald-50/70 border-emerald-200 text-emerald-900'
                    : 'bg-slate-50 border-slate-200 text-slate-800'
                }`}>
                  <p className="text-xs font-semibold uppercase tracking-wider opacity-75">Total Net Variance</p>
                  <div className="flex items-center gap-2 mt-1">
                    {comparison.totalPriceVariance > 0 ? (
                      <TrendingUp className="w-5 h-5 text-rose-600" />
                    ) : comparison.totalPriceVariance < 0 ? (
                      <TrendingDown className="w-5 h-5 text-emerald-600" />
                    ) : (
                      <Minus className="w-5 h-5 text-slate-500" />
                    )}
                    <span className="text-lg font-bold">
                      {comparison.totalPriceVariance > 0 ? '+' : ''}{formatCurrency(comparison.totalPriceVariance)}
                    </span>
                    {comparison.percentageVariance !== 0 && (
                      <span className="text-xs font-semibold px-2 py-0.5 rounded-full bg-white/80">
                        {comparison.percentageVariance > 0 ? '+' : ''}{comparison.percentageVariance}%
                      </span>
                    )}
                  </div>
                </div>
              </div>

              {/* Items Variance Table */}
              <div className="border border-slate-200 rounded-xl overflow-hidden shadow-xs">
                <table className="w-full text-left border-collapse">
                  <thead>
                    <tr className="bg-slate-50 border-b border-slate-200 text-xs font-semibold text-slate-500">
                      <th className="py-2.5 px-3">Product Name</th>
                      <th className="py-2.5 px-3 text-right">Prev Price</th>
                      <th className="py-2.5 px-3 text-right">Current Price</th>
                      <th className="py-2.5 px-3 text-right">Difference</th>
                      <th className="py-2.5 px-3 text-right">% Change</th>
                      <th className="py-2.5 px-3 text-center">Qty Variance</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100 text-sm">
                    {comparison.itemComparisons.map((item, idx) => {
                      const isHike = item.priceDifference > 0;
                      const isDrop = item.priceDifference < 0;
                      return (
                        <tr key={idx} className="hover:bg-slate-50/60 transition">
                          <td className="py-2.5 px-3 font-semibold text-slate-900">{item.productName}</td>
                          <td className="py-2.5 px-3 text-right text-slate-500 font-mono text-xs">
                            {item.previousPrice > 0 ? formatCurrency(item.previousPrice) : '-'}
                          </td>
                          <td className="py-2.5 px-3 text-right font-bold text-slate-900 font-mono text-xs">
                            {formatCurrency(item.currentPrice)}
                          </td>
                          <td className={`py-2.5 px-3 text-right font-semibold text-xs ${
                            isHike ? 'text-rose-600' : isDrop ? 'text-emerald-600' : 'text-slate-500'
                          }`}>
                            {isHike ? `+${formatCurrency(item.priceDifference)}` : formatCurrency(item.priceDifference)}
                          </td>
                          <td className="py-2.5 px-3 text-right">
                            {item.percentageChange !== 0 ? (
                              <span className={`inline-flex items-center gap-0.5 text-xs font-bold px-2 py-0.5 rounded-full ${
                                isHike
                                  ? 'bg-rose-100 text-rose-700'
                                  : 'bg-emerald-100 text-emerald-700'
                              }`}>
                                {isHike ? <TrendingUp className="w-3 h-3" /> : <TrendingDown className="w-3 h-3" />}
                                {isHike ? `+${item.percentageChange}%` : `${item.percentageChange}%`}
                              </span>
                            ) : (
                              <span className="text-xs text-slate-400">0%</span>
                            )}
                          </td>
                          <td className="py-2.5 px-3 text-center text-xs text-slate-600">
                            {item.previousQuantity > 0 ? (
                              <span>{item.previousQuantity} ➔ <strong className="text-slate-900">{item.currentQuantity}</strong> {item.unit}</span>
                            ) : (
                              <span>{item.currentQuantity} {item.unit}</span>
                            )}
                          </td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>
            </>
          )}
        </div>

        {/* Footer */}
        <div className="px-6 py-3 border-t border-slate-100 flex items-center justify-end bg-slate-50/50">
          <button
            onClick={onClose}
            className="px-4 py-2 bg-white border border-slate-200 text-slate-700 hover:bg-slate-50 text-sm font-semibold rounded-lg shadow-xs transition"
          >
            Close
          </button>
        </div>
      </div>
    </div>
  );
}

export default InvoiceComparisonModal;
