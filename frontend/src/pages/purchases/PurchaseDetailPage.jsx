import React from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { ArrowLeft, Printer, Truck, FileText } from 'lucide-react';
import purchaseService from '../../services/purchaseService';
import PageHeader from '../../components/common/PageHeader';
import { formatCurrency, formatDate } from '../../utils/formatters';

export function PurchaseDetailPage() {
  const { id } = useParams();
  const navigate = useNavigate();

  const { data: purchase, isLoading, error } = useQuery({
    queryKey: ['purchases', id],
    queryFn: () => purchaseService.getPurchaseById(id),
  });

  if (isLoading) {
    return (
      <div className="min-h-[400px] flex items-center justify-center">
        <div className="w-8 h-8 border-3 border-indigo-600 border-t-transparent rounded-full animate-spin" />
      </div>
    );
  }

  if (error || !purchase) {
    return (
      <div className="bg-white p-8 rounded-xl border border-slate-200 text-center max-w-lg mx-auto">
        <p className="text-base font-semibold text-slate-800">Purchase Record Not Found</p>
        <button
          onClick={() => navigate('/purchases')}
          className="mt-4 px-4 py-2 bg-indigo-600 text-white rounded-lg text-sm"
        >
          Return to Purchases
        </button>
      </div>
    );
  }

  return (
    <div className="max-w-3xl mx-auto space-y-6">
      <PageHeader
        title={`Purchase Bill #${purchase.invoiceNumber}`}
        subtitle={`Sourced from ${purchase.supplierName || 'Wholesaler'}`}
        breadcrumbs={[
          { label: 'Purchases', to: '/purchases' },
          { label: purchase.invoiceNumber },
        ]}
        action={
          <div className="flex items-center gap-2">
            <button
              onClick={() => navigate('/purchases')}
              className="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold text-slate-700 bg-white border border-slate-200 rounded-lg hover:bg-slate-50 transition"
            >
              <ArrowLeft className="w-3.5 h-3.5" /> Back
            </button>
            <button
              onClick={() => window.print()}
              className="inline-flex items-center gap-1.5 px-3.5 py-1.5 text-xs font-semibold text-white bg-blue-600 hover:bg-blue-700 rounded-lg shadow-sm transition"
            >
              <Printer className="w-3.5 h-3.5" /> Print Bill
            </button>
          </div>
        }
      />

      <div className="bg-white rounded-2xl border border-slate-200 p-8 shadow-sm">
        <div className="flex items-start justify-between pb-6 border-b border-slate-200">
          <div className="flex items-center gap-3">
            <div className="w-12 h-12 rounded-xl bg-blue-600 text-white flex items-center justify-center font-bold">
              <Truck className="w-6 h-6" />
            </div>
            <div>
              <h2 className="text-lg font-bold text-slate-900">{purchase.supplierName || 'Wholesale Supplier'}</h2>
              <p className="text-xs text-slate-500">Consignment Replenishment Receipt</p>
              {purchase.notes && <p className="text-xs text-slate-400 italic">&quot;{purchase.notes}&quot;</p>}
            </div>
          </div>
          <div className="text-right">
            <span className="inline-flex items-center text-xs font-semibold px-2.5 py-1 rounded-full bg-blue-50 text-blue-700 border border-blue-200">
              Goods Received & Logged
            </span>
            <p className="text-xs font-mono text-slate-500 mt-1">Bill: {purchase.invoiceNumber}</p>
            <p className="text-xs text-slate-400">Date: {formatDate(purchase.purchaseDate)}</p>
          </div>
        </div>

        {/* Item table */}
        <div className="py-6">
          <table className="w-full text-left border-collapse text-xs">
            <thead>
              <tr className="border-b border-slate-200 text-slate-500 uppercase tracking-wider font-semibold">
                <th className="py-2.5">Product SKU</th>
                <th className="py-2.5 text-center">Received Units</th>
                <th className="py-2.5 text-right">Unit Wholesale Cost</th>
                <th className="py-2.5 text-right">Total Cost</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {(purchase.items || []).map((item, idx) => (
                <tr key={idx} className="py-3">
                  <td className="py-3 font-semibold text-slate-900">
                    {item.productName}
                    {item.sku && <span className="block text-[10px] text-slate-400 font-mono">{item.sku}</span>}
                  </td>
                  <td className="py-3 text-center text-slate-700 font-medium">
                    +{item.quantity} {item.unit || ''}
                  </td>
                  <td className="py-3 text-right text-slate-600 font-medium">
                    {formatCurrency(item.unitCost)}
                  </td>
                  <td className="py-3 text-right font-bold text-slate-900">
                    {formatCurrency(item.subtotal || item.quantity * item.unitCost)}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        <div className="pt-4 border-t border-slate-200 flex justify-end">
          <div className="w-64 space-y-2 text-xs">
            <div className="pt-2 flex justify-between text-base font-bold text-slate-900">
              <span>Total Sourced Cost:</span>
              <span className="text-blue-700 text-xl">{formatCurrency(purchase.totalAmount)}</span>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

export default PurchaseDetailPage;

