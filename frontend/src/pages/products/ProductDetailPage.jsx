import React, { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { ArrowLeft, Edit2, Boxes, History, Sparkles } from 'lucide-react';
import productService from '../../services/productService';
import categoryService from '../../services/categoryService';
import inventoryService from '../../services/inventoryService';
import PageHeader from '../../components/common/PageHeader';
import StatusBadge from '../../components/common/StatusBadge';
import DataTable from '../../components/common/DataTable';
import ProductFormModal from '../../components/products/ProductFormModal';
import StockAdjustmentModal from '../../components/inventory/StockAdjustmentModal';
import { formatCurrency, formatDateTime } from '../../utils/formatters';
import { getUnitName } from '../../utils/constants';
import { toast } from 'sonner';

export function ProductDetailPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const [isEditOpen, setIsEditOpen] = useState(false);
  const [isAdjustOpen, setIsAdjustOpen] = useState(false);

  const { data: product, isLoading, error } = useQuery({
    queryKey: ['products', id],
    queryFn: () => productService.getProductById(id),
  });

  const { data: categories = [] } = useQuery({
    queryKey: ['categories'],
    queryFn: () => categoryService.getCategories(),
  });

  const { data: history = [] } = useQuery({
    queryKey: ['inventory', 'product', id],
    queryFn: () => inventoryService.getProductHistory(id),
    enabled: !!id,
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }) => productService.updateProduct(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['products'] });
      queryClient.invalidateQueries({ queryKey: ['products', id] });
      toast.success('Product updated!');
      setIsEditOpen(false);
    },
    onError: (err) => {
      toast.error(err.message || 'Failed to update');
    },
  });

  if (isLoading) {
    return (
      <div className="min-h-[400px] flex items-center justify-center">
        <div className="w-8 h-8 border-3 border-indigo-600 border-t-transparent rounded-full animate-spin" />
      </div>
    );
  }

  if (error || !product) {
    return (
      <div className="bg-white p-8 rounded-xl border border-slate-200 text-center">
        <p className="text-base font-semibold text-slate-800">Product Not Found</p>
        <p className="text-sm text-slate-500 mt-1">The requested product could not be found.</p>
        <button
          onClick={() => navigate('/products')}
          className="mt-4 px-4 py-2 bg-indigo-600 text-white rounded-lg text-sm"
        >
          Return to Products
        </button>
      </div>
    );
  }

  const unitDisplay = product.unitName || getUnitName(product.unit);
  const minStock = product.minimumStock ?? product.minStock ?? 5;

  const margin = product.sellingPrice > 0
    ? (((product.sellingPrice - product.purchasePrice) / product.sellingPrice) * 100).toFixed(1)
    : 0;

  const historyColumns = [
    {
      header: 'Date & Time',
      key: 'date',
      render: (r) => formatDateTime(r.date || r.createdAt),
    },
    {
      header: 'Type',
      key: 'type',
      render: (r) => (
        <span
          className={`font-semibold text-xs px-2 py-0.5 rounded-full ${
            r.type === 'Sale' || r.transactionType === 3
              ? 'bg-rose-50 text-rose-700'
              : r.type === 'Purchase' || r.transactionType === 2
              ? 'bg-emerald-50 text-emerald-700'
              : 'bg-amber-50 text-amber-700'
          }`}
        >
          {r.transactionTypeName || r.type || 'Movement'}
        </span>
      ),
    },
    {
      header: 'Quantity Change',
      key: 'quantity',
      align: 'center',
      render: (r) => (
        <span className={`font-bold ${r.quantity > 0 ? 'text-emerald-600' : 'text-slate-800'}`}>
          {r.quantity > 0 ? `+${r.quantity}` : r.quantity} {unitDisplay}
        </span>
      ),
    },
    {
      header: 'Previous Stock',
      key: 'previousStock',
      align: 'center',
      render: (r) => `${r.previousStock} ${unitDisplay}`,
    },
    {
      header: 'New Balance',
      key: 'newStock',
      align: 'center',
      render: (r) => <span className="font-bold text-slate-900">{r.newStock} {unitDisplay}</span>,
    },
    {
      header: 'Audit Reference',
      key: 'reference',
      render: (r) => <span className="text-slate-500 text-xs font-mono">{r.notes || r.reference || r.referenceId || '-'}</span>,
    },
  ];

  return (
    <div className="space-y-6">
      <PageHeader
        title={product.name}
        subtitle={`SKU: ${product.sku} • Category: ${product.categoryName || 'Staples'}`}
        breadcrumbs={[
          { label: 'Products', to: '/products' },
          { label: product.name },
        ]}
        action={
          <div className="flex items-center gap-2">
            <button
              onClick={() => navigate('/products')}
              className="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold text-slate-700 bg-white border border-slate-200 rounded-lg hover:bg-slate-50 transition"
            >
              <ArrowLeft className="w-3.5 h-3.5" /> Back
            </button>
            <button
              onClick={() => setIsAdjustOpen(true)}
              className="inline-flex items-center gap-1.5 px-3.5 py-1.5 text-xs font-semibold text-slate-700 bg-white border border-slate-200 rounded-lg hover:bg-slate-50 shadow-xs transition"
            >
              <Boxes className="w-3.5 h-3.5 text-amber-600" /> Adjust Stock
            </button>
            <button
              onClick={() => setIsEditOpen(true)}
              className="inline-flex items-center gap-1.5 px-3.5 py-1.5 text-xs font-semibold text-white bg-indigo-600 hover:bg-indigo-700 rounded-lg shadow-sm transition"
            >
              <Edit2 className="w-3.5 h-3.5" /> Edit Product
            </button>
          </div>
        }
      />

      {/* Main product overview cards */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        <div className="bg-white p-5 rounded-xl border border-slate-200 shadow-xs">
          <p className="text-xs font-semibold text-slate-500 uppercase tracking-wider">Current Stock</p>
          <div className="mt-2 flex items-baseline gap-2">
            <span className="text-2xl font-bold text-slate-900">
              {product.currentStock} {unitDisplay}
            </span>
            <StatusBadge
              status={
                product.currentStock === 0
                  ? 'Out of Stock'
                  : product.currentStock <= minStock
                  ? 'Low Stock'
                  : 'In Stock'
              }
              size="sm"
            />
          </div>
          <p className="text-xs text-slate-500 mt-2">
            Min Alert Threshold: <span className="font-semibold text-slate-700">{minStock} {unitDisplay}</span>
          </p>
        </div>

        <div className="bg-white p-5 rounded-xl border border-slate-200 shadow-xs">
          <p className="text-xs font-semibold text-slate-500 uppercase tracking-wider">Retail Selling Price</p>
          <div className="mt-2 text-2xl font-bold text-slate-900">
            {formatCurrency(product.sellingPrice)}
          </div>
          <p className="text-xs text-slate-500 mt-2">
            Wholesale Cost: <span className="font-semibold text-slate-700">{formatCurrency(product.purchasePrice)}</span>
          </p>
        </div>

        <div className="bg-white p-5 rounded-xl border border-slate-200 shadow-xs">
          <p className="text-xs font-semibold text-slate-500 uppercase tracking-wider">Gross Profit Margin</p>
          <div className="mt-2 flex items-baseline gap-2">
            <span className="text-2xl font-bold text-emerald-600">+{margin}%</span>
            <span className="text-xs font-semibold text-slate-600">
              ({formatCurrency(product.sellingPrice - product.purchasePrice)}/unit)
            </span>
          </div>
          <p className="text-xs text-slate-500 mt-2">Calculated on retail selling price</p>
        </div>

        <div className="bg-white p-5 rounded-xl border border-slate-200 shadow-xs">
          <p className="text-xs font-semibold text-slate-500 uppercase tracking-wider">AI Restock Advice</p>
          <div className="mt-2 flex items-center gap-1.5 text-indigo-700 font-semibold text-sm">
            <Sparkles className="w-4 h-4 text-amber-500" />
            <span>Avg {product.avgDailySales || product.averageDailySales || 2.0} units/day</span>
          </div>
          <p className="text-xs text-slate-500 mt-2">
            Estimated coverage: ~{product.avgDailySales ? (product.currentStock / product.avgDailySales).toFixed(1) : 0} days
          </p>
        </div>
      </div>

      {/* Stock Transaction Ledger for this product */}
      <div className="bg-white rounded-xl border border-slate-200 p-5 shadow-xs">
        <div className="flex items-center justify-between mb-4">
          <div className="flex items-center gap-2">
            <History className="w-5 h-5 text-slate-500" />
            <div>
              <h3 className="text-base font-bold text-slate-900">Stock Movement History</h3>
              <p className="text-xs text-slate-500">Timeline audit trail for this SKU from backend inventory ledger</p>
            </div>
          </div>
        </div>

        <DataTable
          columns={historyColumns}
          data={history}
          emptyTitle="No stock movements recorded yet"
          emptyDescription="Transactions will appear here when you record sales, purchases, or adjustments."
        />
      </div>

      {/* Modals */}
      {isEditOpen && (
        <ProductFormModal
          isOpen={isEditOpen}
          onClose={() => setIsEditOpen(false)}
          initialData={product}
          categories={categories}
          isLoading={updateMutation.isPending}
          onSubmit={(data) => updateMutation.mutate({ id: product.id, data })}
        />
      )}

      {isAdjustOpen && (
        <StockAdjustmentModal
          isOpen={isAdjustOpen}
          onClose={() => setIsAdjustOpen(false)}
          initialProduct={product}
        />
      )}
    </div>
  );
}

export default ProductDetailPage;
