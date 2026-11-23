import React, { useState } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import inventoryService from '../../services/inventoryService';
import { X, Loader2, ArrowUp, ArrowDown, RefreshCw } from 'lucide-react';
import { toast } from 'sonner';

export function StockAdjustmentModal({
  isOpen,
  onClose,
  initialProduct = null,
  products = [],
}) {
  const queryClient = useQueryClient();

  const [selectedProductId, setSelectedProductId] = useState(
    initialProduct?.id ? String(initialProduct.id) : products[0]?.id ? String(products[0].id) : ''
  );
  const [adjustmentType, setAdjustmentType] = useState('Add'); // 'Add' | 'Subtract' | 'Set'
  const [quantity, setQuantity] = useState('');
  const [reason, setReason] = useState('Damaged packets discarded during weekly inspection');

  const currentProduct =
    initialProduct ||
    products.find((p) => String(p.id) === String(selectedProductId));

  const adjustMutation = useMutation({
    mutationFn: (data) => inventoryService.createAdjustment(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['inventory'] });
      queryClient.invalidateQueries({ queryKey: ['products'] });
      queryClient.invalidateQueries({ queryKey: ['dashboard'] });
      toast.success('Inventory stock adjusted successfully!');
      onClose();
    },
    onError: (err) => {
      toast.error(err.message || 'Failed to adjust stock');
    },
  });

  if (!isOpen) return null;

  const calculatedNewStock = () => {
    if (!currentProduct) return 0;
    const prev = currentProduct.currentStock || 0;
    const qty = Number(quantity) || 0;
    if (adjustmentType === 'Add') return prev + qty;
    if (adjustmentType === 'Subtract') return Math.max(0, prev - qty);
    return Math.max(0, qty);
  };

  const handleSubmit = (e) => {
    e.preventDefault();
    if (!selectedProductId && !initialProduct) {
      toast.error('Please select a product');
      return;
    }
    const numQty = Number(quantity);
    if (isNaN(numQty) || numQty < 0) {
      toast.error('Please enter a valid quantity');
      return;
    }

    const finalNewQuantity = calculatedNewStock();

    // Call backend POST /api/v1/inventory/adjustment
    adjustMutation.mutate({
      productId: currentProduct?.id || selectedProductId,
      newQuantity: finalNewQuantity,
      reason: `${reason} (${adjustmentType === 'Add' ? `+${numQty}` : adjustmentType === 'Subtract' ? `-${numQty}` : `Set to ${numQty}`})`,
    });
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/50 backdrop-blur-sm p-4 animate-in fade-in">
      <div className="bg-white rounded-2xl max-w-md w-full p-6 shadow-xl border border-slate-100 relative">
        <button
          onClick={onClose}
          className="absolute top-4 right-4 text-slate-400 hover:text-slate-600"
        >
          <X className="w-5 h-5" />
        </button>

        <h3 className="text-lg font-bold text-slate-900 mb-1">Adjust Inventory Stock</h3>
        <p className="text-xs text-slate-500 mb-4">
          Record physical count discrepancies, damaged goods, or expired shelf stock.
        </p>

        <form onSubmit={handleSubmit} className="space-y-4">
          {!initialProduct && (
            <div>
              <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1">
                Select Product *
              </label>
              <select
                value={selectedProductId}
                onChange={(e) => setSelectedProductId(e.target.value)}
                className="w-full px-3 py-2 text-sm bg-white border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-600"
              >
                {products.map((p) => (
                  <option key={p.id} value={p.id}>
                    {p.name} (Current: {p.currentStock} {p.unitName || ''})
                  </option>
                ))}
              </select>
            </div>
          )}

          {currentProduct && (
            <div className="p-3 bg-slate-50 rounded-lg border border-slate-200 text-xs flex justify-between items-center">
              <div>
                <p className="font-semibold text-slate-900">{currentProduct.name}</p>
                <p className="text-slate-500">
                  Current Balance: <span className="font-bold text-slate-800">{currentProduct.currentStock} {currentProduct.unitName || ''}</span>
                </p>
              </div>
              <div className="text-right">
                <span className="text-[11px] text-slate-400">Target New Stock:</span>
                <p className="font-bold text-indigo-700 text-sm">
                  {calculatedNewStock()} {currentProduct.unitName || ''}
                </p>
              </div>
            </div>
          )}

          <div>
            <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1.5">
              Adjustment Type *
            </label>
            <div className="grid grid-cols-3 gap-2">
              <button
                type="button"
                onClick={() => setAdjustmentType('Add')}
                className={`py-2 px-3 rounded-lg border text-xs font-semibold flex items-center justify-center gap-1.5 transition ${
                  adjustmentType === 'Add'
                    ? 'bg-emerald-50 border-emerald-300 text-emerald-800'
                    : 'bg-white border-slate-200 text-slate-600 hover:bg-slate-50'
                }`}
              >
                <ArrowUp className="w-3.5 h-3.5 text-emerald-600" /> Add (+)
              </button>
              <button
                type="button"
                onClick={() => setAdjustmentType('Subtract')}
                className={`py-2 px-3 rounded-lg border text-xs font-semibold flex items-center justify-center gap-1.5 transition ${
                  adjustmentType === 'Subtract'
                    ? 'bg-rose-50 border-rose-300 text-rose-800'
                    : 'bg-white border-slate-200 text-slate-600 hover:bg-slate-50'
                }`}
              >
                <ArrowDown className="w-3.5 h-3.5 text-rose-600" /> Deduct (-)
              </button>
              <button
                type="button"
                onClick={() => setAdjustmentType('Set')}
                className={`py-2 px-3 rounded-lg border text-xs font-semibold flex items-center justify-center gap-1.5 transition ${
                  adjustmentType === 'Set'
                    ? 'bg-indigo-50 border-indigo-300 text-indigo-800'
                    : 'bg-white border-slate-200 text-slate-600 hover:bg-slate-50'
                }`}
              >
                <RefreshCw className="w-3.5 h-3.5 text-indigo-600" /> Direct Set
              </button>
            </div>
          </div>

          <div>
            <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1">
              Quantity to {adjustmentType === 'Set' ? 'Set As' : adjustmentType} *
            </label>
            <input
              type="number"
              min="0"
              value={quantity}
              onChange={(e) => setQuantity(e.target.value)}
              placeholder="e.g. 5"
              className="w-full px-3 py-2 text-sm bg-white border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-600"
              required
            />
          </div>

          <div>
            <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1">
              Audit Reason *
            </label>
            <select
              value={reason}
              onChange={(e) => setReason(e.target.value)}
              className="w-full px-3 py-2 text-sm bg-white border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-600"
            >
              <option value="Damaged packets discarded during weekly inspection">
                Damaged packets discarded during inspection
              </option>
              <option value="Expired stock discarded during defrosting">
                Expired stock discarded / spoiled
              </option>
              <option value="Physical shelf audit reconciliation">
                Physical shelf audit reconciliation
              </option>
              <option value="Customer return accepted back to inventory">
                Customer return accepted back to inventory
              </option>
              <option value="Internal Kirana usage / promotional sample">
                Internal Kirana usage / promotional sample
              </option>
            </select>
          </div>

          <div className="pt-3 border-t border-slate-100 flex items-center justify-end gap-2">
            <button
              type="button"
              onClick={onClose}
              disabled={adjustMutation.isPending}
              className="px-4 py-2 text-sm font-medium text-slate-700 bg-white border border-slate-200 rounded-lg hover:bg-slate-50"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={adjustMutation.isPending || !quantity}
              className="px-5 py-2 text-sm font-semibold text-white bg-indigo-600 hover:bg-indigo-700 rounded-lg shadow-sm flex items-center gap-2 disabled:opacity-50"
            >
              {adjustMutation.isPending && <Loader2 className="w-4 h-4 animate-spin" />}
              Save Stock Adjustment
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

export default StockAdjustmentModal;
