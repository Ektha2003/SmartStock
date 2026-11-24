import React, { useState } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import {
  ArrowLeft,
  Building2,
  Phone,
  Mail,
  MapPin,
  CreditCard,
  FileText,
  Truck,
  Boxes,
  Calendar,
  ExternalLink,
  Receipt,
  FileSpreadsheet,
  AlertCircle
} from 'lucide-react';
import supplierService from '../../services/supplierService';
import PageHeader from '../../components/common/PageHeader';
import StatusBadge from '../../components/common/StatusBadge';
import { formatCurrency, formatDate } from '../../utils/formatters';

export function SupplierDetailPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const [activeTab, setActiveTab] = useState('products');

  const { data: supplier, isLoading, error } = useQuery({
    queryKey: ['supplier-detail', id],
    queryFn: () => supplierService.getSupplierDetail(id),
    enabled: !!id,
  });

  if (isLoading) {
    return (
      <div className="flex items-center justify-center min-h-[350px]">
        <div className="flex flex-col items-center gap-3">
          <div className="w-8 h-8 border-3 border-indigo-600 border-t-transparent rounded-full animate-spin" />
          <p className="text-sm text-slate-500">Loading supplier profile...</p>
        </div>
      </div>
    );
  }

  if (error || !supplier) {
    return (
      <div className="bg-white p-8 rounded-xl border border-slate-200 text-center max-w-md mx-auto my-12">
        <AlertCircle className="w-10 h-10 text-rose-500 mx-auto mb-3" />
        <h3 className="text-base font-semibold text-slate-800">Supplier Not Found</h3>
        <p className="text-sm text-slate-500 mt-1 mb-5">The requested supplier could not be loaded or was removed.</p>
        <button
          onClick={() => navigate('/suppliers')}
          className="inline-flex items-center gap-2 px-4 py-2 bg-indigo-600 text-white text-sm font-medium rounded-lg"
        >
          <ArrowLeft className="w-4 h-4" />
          Back to Directory
        </button>
      </div>
    );
  }

  const products = supplier.suppliedProducts || [];
  const invoices = supplier.recentInvoices || [];
  const purchases = supplier.recentPurchases || [];

  return (
    <div className="space-y-6">
      <PageHeader
        title={supplier.supplierName}
        subtitle={supplier.companyName || 'Registered Wholesale Vendor'}
        breadcrumbs={[
          { label: 'Suppliers', href: '/suppliers' },
          { label: supplier.supplierName },
        ]}
        action={
          <div className="flex items-center gap-2">
            <Link
              to={`/supplier-invoices?supplierId=${supplier.id}`}
              className="inline-flex items-center gap-2 px-3.5 py-2 bg-indigo-50 text-indigo-700 hover:bg-indigo-100 text-sm font-medium rounded-lg transition"
            >
              <FileSpreadsheet className="w-4 h-4" />
              <span>Supplier Invoices</span>
            </Link>
            <button
              onClick={() => navigate('/suppliers')}
              className="inline-flex items-center gap-2 px-3.5 py-2 bg-white border border-slate-200 text-slate-700 hover:bg-slate-50 text-sm font-medium rounded-lg shadow-xs transition"
            >
              <ArrowLeft className="w-4 h-4" />
              <span>Back</span>
            </button>
          </div>
        }
      />

      {/* Supplier Profile Overview Cards */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-5">
        {/* Contact & Business Info */}
        <div className="bg-white p-5 rounded-xl border border-slate-200 shadow-xs space-y-4">
          <div className="flex items-center justify-between border-b border-slate-100 pb-3">
            <h3 className="text-sm font-semibold text-slate-900 flex items-center gap-2">
              <Building2 className="w-4 h-4 text-indigo-600" />
              Vendor Details
            </h3>
            <StatusBadge status={supplier.status || 'Active'} size="sm" />
          </div>

          <div className="space-y-3 text-sm">
            <div>
              <p className="text-xs text-slate-400 font-medium">Company Name</p>
              <p className="text-slate-800 font-medium">{supplier.companyName || 'Not specified'}</p>
            </div>
            <div>
              <p className="text-xs text-slate-400 font-medium">Contact Person</p>
              <p className="text-slate-800 font-medium">{supplier.contactPerson || 'Not specified'}</p>
            </div>
            <div className="grid grid-cols-2 gap-2">
              <div>
                <p className="text-xs text-slate-400 font-medium">Phone</p>
                <p className="text-slate-800 flex items-center gap-1.5 mt-0.5">
                  <Phone className="w-3.5 h-3.5 text-slate-400" />
                  <span>{supplier.phone || '-'}</span>
                </p>
              </div>
              <div>
                <p className="text-xs text-slate-400 font-medium">Email</p>
                <p className="text-slate-800 flex items-center gap-1.5 mt-0.5 truncate" title={supplier.email}>
                  <Mail className="w-3.5 h-3.5 text-slate-400 shrink-0" />
                  <span className="truncate">{supplier.email || '-'}</span>
                </p>
              </div>
            </div>
          </div>
        </div>

        {/* Tax & Financial Details */}
        <div className="bg-white p-5 rounded-xl border border-slate-200 shadow-xs space-y-4">
          <div className="flex items-center justify-between border-b border-slate-100 pb-3">
            <h3 className="text-sm font-semibold text-slate-900 flex items-center gap-2">
              <CreditCard className="w-4 h-4 text-indigo-600" />
              Tax & Payment Terms
            </h3>
          </div>

          <div className="space-y-3 text-sm">
            <div className="grid grid-cols-2 gap-2">
              <div>
                <p className="text-xs text-slate-400 font-medium">GSTIN</p>
                <p className="text-slate-800 font-mono font-semibold text-xs mt-0.5 bg-slate-50 px-2 py-1 rounded inline-block">
                  {supplier.gstNumber || supplier.gstin || 'Unregistered'}
                </p>
              </div>
              <div>
                <p className="text-xs text-slate-400 font-medium">PAN</p>
                <p className="text-slate-800 font-mono text-xs mt-0.5">
                  {supplier.panNumber || 'Not provided'}
                </p>
              </div>
            </div>
            <div>
              <p className="text-xs text-slate-400 font-medium">Payment Terms</p>
              <p className="text-slate-800 font-medium">{supplier.paymentTerms || 'Standard (Net 30)'}</p>
            </div>
            <div>
              <p className="text-xs text-slate-400 font-medium">Billing Address</p>
              <p className="text-slate-600 text-xs mt-0.5">{supplier.billingAddress || supplier.address || 'Same as primary'}</p>
            </div>
          </div>
        </div>

        {/* Activity Summary Stats */}
        <div className="bg-white p-5 rounded-xl border border-slate-200 shadow-xs space-y-4">
          <h3 className="text-sm font-semibold text-slate-900 flex items-center gap-2 border-b border-slate-100 pb-3">
            <Truck className="w-4 h-4 text-indigo-600" />
            Supply Statistics
          </h3>

          <div className="grid grid-cols-3 gap-3 text-center">
            <div className="bg-slate-50 p-3 rounded-lg border border-slate-100">
              <p className="text-xs text-slate-500 font-medium">Products</p>
              <p className="text-lg font-bold text-slate-900 mt-1">{products.length}</p>
            </div>
            <div className="bg-slate-50 p-3 rounded-lg border border-slate-100">
              <p className="text-xs text-slate-500 font-medium">Invoices</p>
              <p className="text-lg font-bold text-indigo-600 mt-1">{invoices.length}</p>
            </div>
            <div className="bg-slate-50 p-3 rounded-lg border border-slate-100">
              <p className="text-xs text-slate-500 font-medium">Purchases</p>
              <p className="text-lg font-bold text-emerald-600 mt-1">{purchases.length}</p>
            </div>
          </div>

          {supplier.notes && (
            <div className="bg-amber-50/70 p-3 rounded-lg border border-amber-200/60 text-xs text-amber-900">
              <span className="font-semibold">Notes:</span> {supplier.notes}
            </div>
          )}
        </div>
      </div>

      {/* Tabs Navigation */}
      <div className="bg-white rounded-xl border border-slate-200 shadow-xs overflow-hidden">
        <div className="flex border-b border-slate-200 px-4 bg-slate-50/70 gap-2">
          <button
            onClick={() => setActiveTab('products')}
            className={`py-3 px-4 text-sm font-semibold flex items-center gap-2 border-b-2 transition ${
              activeTab === 'products'
                ? 'border-indigo-600 text-indigo-600 bg-white'
                : 'border-transparent text-slate-600 hover:text-slate-900'
            }`}
          >
            <Boxes className="w-4 h-4" />
            Supplied Products ({products.length})
          </button>
          <button
            onClick={() => setActiveTab('invoices')}
            className={`py-3 px-4 text-sm font-semibold flex items-center gap-2 border-b-2 transition ${
              activeTab === 'invoices'
                ? 'border-indigo-600 text-indigo-600 bg-white'
                : 'border-transparent text-slate-600 hover:text-slate-900'
            }`}
          >
            <FileSpreadsheet className="w-4 h-4" />
            Supplier Invoices ({invoices.length})
          </button>
          <button
            onClick={() => setActiveTab('purchases')}
            className={`py-3 px-4 text-sm font-semibold flex items-center gap-2 border-b-2 transition ${
              activeTab === 'purchases'
                ? 'border-indigo-600 text-indigo-600 bg-white'
                : 'border-transparent text-slate-600 hover:text-slate-900'
            }`}
          >
            <Receipt className="w-4 h-4" />
            Purchase Orders ({purchases.length})
          </button>
        </div>

        {/* Tab 1: Products Supplied */}
        {activeTab === 'products' && (
          <div className="p-4">
            {products.length === 0 ? (
              <div className="text-center py-12 text-slate-500">
                <Boxes className="w-10 h-10 text-slate-300 mx-auto mb-2" />
                <p className="text-sm font-medium">No products linked to this supplier yet.</p>
                <p className="text-xs text-slate-400 mt-1">Products will appear here once purchases or supplier invoices are approved.</p>
              </div>
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full text-left border-collapse">
                  <thead>
                    <tr className="border-b border-slate-200 text-xs font-semibold text-slate-500 bg-slate-50">
                      <th className="py-2.5 px-3">Product Name</th>
                      <th className="py-2.5 px-3">SKU</th>
                      <th className="py-2.5 px-3">Category</th>
                      <th className="py-2.5 px-3 text-right">Current Stock</th>
                      <th className="py-2.5 px-3 text-right">Last Purchase Price</th>
                      <th className="py-2.5 px-3 text-right">Last Purchased</th>
                      <th className="py-2.5 px-3 text-right">Action</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100 text-sm">
                    {products.map((p) => (
                      <tr key={p.productId} className="hover:bg-slate-50/80 transition">
                        <td className="py-2.5 px-3 font-semibold text-slate-900">{p.productName}</td>
                        <td className="py-2.5 px-3 font-mono text-xs text-slate-600">{p.sku}</td>
                        <td className="py-2.5 px-3 text-slate-600 text-xs">{p.categoryName}</td>
                        <td className="py-2.5 px-3 text-right font-medium text-slate-800">
                          {p.currentStock} {p.unit}
                        </td>
                        <td className="py-2.5 px-3 text-right font-semibold text-slate-900">
                          {formatCurrency(p.lastPurchasePrice)}
                        </td>
                        <td className="py-2.5 px-3 text-right text-xs text-slate-500">
                          {p.lastPurchasedAt ? formatDate(p.lastPurchasedAt) : '-'}
                        </td>
                        <td className="py-2.5 px-3 text-right">
                          <Link
                            to={`/products/${p.productId}`}
                            className="text-xs text-indigo-600 hover:text-indigo-800 font-medium inline-flex items-center gap-1"
                          >
                            <span>View</span>
                            <ExternalLink className="w-3 h-3" />
                          </Link>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        )}

        {/* Tab 2: Invoices */}
        {activeTab === 'invoices' && (
          <div className="p-4">
            {invoices.length === 0 ? (
              <div className="text-center py-12 text-slate-500">
                <FileSpreadsheet className="w-10 h-10 text-slate-300 mx-auto mb-2" />
                <p className="text-sm font-medium">No invoices uploaded for this supplier yet.</p>
                <Link
                  to="/supplier-invoices"
                  className="inline-flex items-center gap-1.5 px-3 py-1.5 bg-indigo-600 text-white text-xs font-semibold rounded-lg mt-3"
                >
                  Upload Invoice Now
                </Link>
              </div>
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full text-left border-collapse">
                  <thead>
                    <tr className="border-b border-slate-200 text-xs font-semibold text-slate-500 bg-slate-50">
                      <th className="py-2.5 px-3">Invoice Number</th>
                      <th className="py-2.5 px-3">Invoice Date</th>
                      <th className="py-2.5 px-3 text-center">Items</th>
                      <th className="py-2.5 px-3 text-right">Grand Total</th>
                      <th className="py-2.5 px-3 text-center">Status</th>
                      <th className="py-2.5 px-3 text-right">Action</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100 text-sm">
                    {invoices.map((inv) => (
                      <tr key={inv.invoiceId} className="hover:bg-slate-50/80 transition">
                        <td className="py-2.5 px-3 font-semibold text-indigo-600">
                          <Link to={`/supplier-invoices/${inv.invoiceId}/review`} className="hover:underline">
                            #{inv.invoiceNumber}
                          </Link>
                        </td>
                        <td className="py-2.5 px-3 text-slate-600 text-xs">{formatDate(inv.invoiceDate)}</td>
                        <td className="py-2.5 px-3 text-center font-medium">{inv.itemCount}</td>
                        <td className="py-2.5 px-3 text-right font-bold text-slate-900">{formatCurrency(inv.grandTotal)}</td>
                        <td className="py-2.5 px-3 text-center">
                          <StatusBadge status={inv.status} size="sm" />
                        </td>
                        <td className="py-2.5 px-3 text-right">
                          <Link
                            to={`/supplier-invoices/${inv.invoiceId}/review`}
                            className="px-2.5 py-1 text-xs font-semibold bg-slate-100 hover:bg-slate-200 text-slate-800 rounded-md transition"
                          >
                            Review & Match
                          </Link>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        )}

        {/* Tab 3: Purchases */}
        {activeTab === 'purchases' && (
          <div className="p-4">
            {purchases.length === 0 ? (
              <div className="text-center py-12 text-slate-500">
                <Receipt className="w-10 h-10 text-slate-300 mx-auto mb-2" />
                <p className="text-sm font-medium">No purchase transactions recorded yet.</p>
              </div>
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full text-left border-collapse">
                  <thead>
                    <tr className="border-b border-slate-200 text-xs font-semibold text-slate-500 bg-slate-50">
                      <th className="py-2.5 px-3">Purchase Invoice</th>
                      <th className="py-2.5 px-3">Purchase Date</th>
                      <th className="py-2.5 px-3 text-center">Item Count</th>
                      <th className="py-2.5 px-3 text-right">Total Amount</th>
                      <th className="py-2.5 px-3 text-right">Action</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100 text-sm">
                    {purchases.map((pur) => (
                      <tr key={pur.purchaseId} className="hover:bg-slate-50/80 transition">
                        <td className="py-2.5 px-3 font-semibold text-slate-900">{pur.invoiceNumber}</td>
                        <td className="py-2.5 px-3 text-slate-600 text-xs">{formatDate(pur.purchaseDate)}</td>
                        <td className="py-2.5 px-3 text-center font-medium">{pur.itemCount}</td>
                        <td className="py-2.5 px-3 text-right font-bold text-slate-900">{formatCurrency(pur.totalAmount)}</td>
                        <td className="py-2.5 px-3 text-right">
                          <Link
                            to={`/purchases/${pur.purchaseId}`}
                            className="text-xs text-indigo-600 hover:text-indigo-800 font-medium inline-flex items-center gap-1"
                          >
                            <span>View Details</span>
                            <ExternalLink className="w-3 h-3" />
                          </Link>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  );
}

export default SupplierDetailPage;
