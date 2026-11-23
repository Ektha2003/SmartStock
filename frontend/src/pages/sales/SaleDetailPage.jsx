import React from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { ArrowLeft, Printer, ShoppingCart, Store, CheckCircle } from 'lucide-react';
import salesService from '../../services/salesService';
import PageHeader from '../../components/common/PageHeader';
import { formatCurrency, formatDateTime } from '../../utils/formatters';

export function SaleDetailPage() {
  const { id } = useParams();
  const navigate = useNavigate();

  const { data: sale, isLoading, error } = useQuery({
    queryKey: ['sales', id],
    queryFn: () => salesService.getSaleById(id),
  });

  if (isLoading) {
    return (
      <div className="min-h-[400px] flex items-center justify-center">
        <div className="w-8 h-8 border-3 border-indigo-600 border-t-transparent rounded-full animate-spin" />
      </div>
    );
  }

  if (error || !sale) {
    return (
      <div className="bg-white p-8 rounded-xl border border-slate-200 text-center max-w-lg mx-auto">
        <p className="text-base font-semibold text-slate-800">Invoice Not Found</p>
        <p className="text-sm text-slate-500 mt-1">The requested sales invoice could not be located.</p>
        <button
          onClick={() => navigate('/sales')}
          className="mt-4 px-4 py-2 bg-indigo-600 text-white rounded-lg text-sm font-medium"
        >
          Return to Sales
        </button>
      </div>
    );
  }

  return (
    <div className="max-w-3xl mx-auto space-y-6">
      <PageHeader
        title={`Invoice ${sale.invoiceNumber}`}
        subtitle={`Recorded on ${formatDateTime(sale.date)}`}
        breadcrumbs={[
          { label: 'Sales', to: '/sales' },
          { label: sale.invoiceNumber },
        ]}
        action={
          <div className="flex items-center gap-2">
            <button
              onClick={() => navigate('/sales')}
              className="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold text-slate-700 bg-white border border-slate-200 rounded-lg hover:bg-slate-50 transition"
            >
              <ArrowLeft className="w-3.5 h-3.5" /> Back
            </button>
            <button
              onClick={() => window.print()}
              className="inline-flex items-center gap-1.5 px-3.5 py-1.5 text-xs font-semibold text-white bg-indigo-600 hover:bg-indigo-700 rounded-lg shadow-sm transition"
            >
              <Printer className="w-3.5 h-3.5" /> Print Receipt
            </button>
          </div>
        }
      />

      {/* Invoice Card */}
      <div className="bg-white rounded-2xl border border-slate-200 p-8 shadow-sm print:border-none print:shadow-none">
        {/* Header */}
        <div className="flex items-start justify-between pb-6 border-b border-slate-200">
          <div className="flex items-center gap-3">
            <div className="w-12 h-12 rounded-xl bg-indigo-600 text-white flex items-center justify-center font-bold">
              <Store className="w-6 h-6" />
            </div>
            <div>
              <h2 className="text-lg font-bold text-slate-900">SmartStock Kirana Store</h2>
              <p className="text-xs text-slate-500">Retail Grocery & Staples</p>
              <p className="text-xs text-slate-400">GSTIN: 07AABCS1429B1Z8</p>
            </div>
          </div>
          <div className="text-right">
            <span className="inline-flex items-center gap-1 text-xs font-semibold px-2.5 py-1 rounded-full bg-emerald-50 text-emerald-700 border border-emerald-200">
              <CheckCircle className="w-3.5 h-3.5" /> Paid via {sale.paymentMethod || 'Cash'}
            </span>
            <p className="text-xs font-mono text-slate-500 mt-1">{sale.invoiceNumber}</p>
          </div>
        </div>

        {/* Customer & Timestamp row */}
        <div className="grid grid-cols-2 gap-4 py-5 border-b border-slate-100 text-xs">
          <div>
            <p className="text-slate-400 font-semibold uppercase tracking-wider mb-1">Billed To:</p>
            <p className="font-bold text-slate-900 text-sm">{sale.customerName || 'Walk-in Customer'}</p>
            {sale.customerPhone && <p className="text-slate-500">{sale.customerPhone}</p>}
          </div>
          <div className="text-right">
            <p className="text-slate-400 font-semibold uppercase tracking-wider mb-1">Invoice Date & Time:</p>
            <p className="font-semibold text-slate-800">{formatDateTime(sale.date)}</p>
            {sale.notes && <p className="text-slate-500 mt-1 italic">&quot;{sale.notes}&quot;</p>}
          </div>
        </div>

        {/* Items Table */}
        <div className="py-6">
          <table className="w-full text-left border-collapse text-xs">
            <thead>
              <tr className="border-b border-slate-200 text-slate-500 uppercase tracking-wider font-semibold">
                <th className="py-2.5">Item Description</th>
                <th className="py-2.5 text-center">Qty</th>
                <th className="py-2.5 text-right">Unit Price</th>
                <th className="py-2.5 text-right">Subtotal</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {(sale.items || []).map((item, idx) => (
                <tr key={idx} className="py-3">
                  <td className="py-3 font-semibold text-slate-900">
                    {item.productName}
                    {item.sku && <span className="block text-[10px] text-slate-400 font-mono">{item.sku}</span>}
                  </td>
                  <td className="py-3 text-center text-slate-700 font-medium">
                    {item.quantity} {item.unit || ''}
                  </td>
                  <td className="py-3 text-right text-slate-600 font-medium">
                    {formatCurrency(item.unitPrice)}
                  </td>
                  <td className="py-3 text-right font-bold text-slate-900">
                    {formatCurrency(item.subtotal || item.quantity * item.unitPrice)}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        {/* Total Summary */}
        <div className="pt-4 border-t border-slate-200 flex justify-end">
          <div className="w-64 space-y-2 text-xs">
            <div className="flex justify-between text-slate-500">
              <span>Subtotal:</span>
              <span className="font-medium text-slate-800">{formatCurrency(sale.totalAmount)}</span>
            </div>
            <div className="flex justify-between text-slate-500">
              <span>Taxes (Included):</span>
              <span className="font-medium text-slate-800">₹0.00</span>
            </div>
            <div className="pt-2 border-t border-slate-200 flex justify-between text-base font-bold text-slate-900">
              <span>Total Paid:</span>
              <span className="text-indigo-700 text-xl">{formatCurrency(sale.totalAmount)}</span>
            </div>
          </div>
        </div>

        {/* Footer */}
        <div className="mt-8 pt-6 border-t border-slate-100 text-center text-[11px] text-slate-400">
          <p>Thank you for shopping at SmartStock Kirana! Please visit again.</p>
        </div>
      </div>
    </div>
  );
}

export default SaleDetailPage;

