import React from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { productSchema } from '../../validations/productSchemas';
import { UnitType, UnitTypeName } from '../../utils/constants';
import { X, Loader2 } from 'lucide-react';

export function ProductFormModal({
  isOpen,
  onClose,
  onSubmit,
  initialData = null,
  categories = [],
  isLoading = false,
}) {
  const isEditing = !!initialData?.id;

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm({
    resolver: zodResolver(productSchema),
    defaultValues: {
      name: initialData?.name || '',
      sku: initialData?.sku || '',
      categoryId: initialData?.categoryId || '',
      description: initialData?.description || '',
      purchasePrice: initialData?.purchasePrice ?? '',
      sellingPrice: initialData?.sellingPrice ?? '',
      currentStock: initialData?.currentStock ?? 0,
      minStock: initialData?.minimumStock ?? initialData?.minStock ?? 5,
      unit: initialData?.unit || 1,
      status: initialData?.status || 'Active',
    },
  });

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/50 backdrop-blur-sm p-4 animate-in fade-in">
      <div className="bg-white rounded-2xl max-w-xl w-full p-6 shadow-xl border border-slate-100 max-h-[90vh] overflow-y-auto relative">
        <button
          onClick={onClose}
          className="absolute top-4 right-4 text-slate-400 hover:text-slate-600"
        >
          <X className="w-5 h-5" />
        </button>

        <h3 className="text-lg font-bold text-slate-900 mb-1">
          {isEditing ? 'Edit Product' : 'Add New Product'}
        </h3>
        <p className="text-xs text-slate-500 mb-5">
          {isEditing
            ? 'Update pricing, minimum thresholds, or details for this item.'
            : 'Fill in details to register a new SKU into your inventory.'}
        </p>

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div className="sm:col-span-2">
              <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1">
                Product Name *
              </label>
              <input
                type="text"
                {...register('name')}
                placeholder="e.g. Aashirvaad Shudh Chakki Atta 10kg"
                className="w-full px-3 py-2 text-sm bg-white border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-600"
              />
              {errors.name && <p className="text-xs text-rose-600 mt-1">{errors.name.message}</p>}
            </div>

            <div>
              <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1">
                SKU / Barcode *
              </label>
              <input
                type="text"
                {...register('sku')}
                placeholder="e.g. AASH-ATTA-10K"
                className="w-full px-3 py-2 text-sm bg-white border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-600"
              />
              {errors.sku && <p className="text-xs text-rose-600 mt-1">{errors.sku.message}</p>}
            </div>

            <div>
              <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1">
                Category *
              </label>
              <select
                {...register('categoryId')}
                className="w-full px-3 py-2 text-sm bg-white border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-600"
              >
                <option value="">Select category...</option>
                {categories.map((c) => (
                  <option key={c.id} value={c.id}>
                    {c.name}
                  </option>
                ))}
              </select>
              {errors.categoryId && (
                <p className="text-xs text-rose-600 mt-1">{errors.categoryId.message}</p>
              )}
            </div>

            <div>
              <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1">
                Wholesale Purchase Cost (₹) *
              </label>
              <input
                type="number"
                step="any"
                {...register('purchasePrice')}
                placeholder="e.g. 420"
                className="w-full px-3 py-2 text-sm bg-white border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-600"
              />
              {errors.purchasePrice && (
                <p className="text-xs text-rose-600 mt-1">{errors.purchasePrice.message}</p>
              )}
            </div>

            <div>
              <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1">
                Retail Selling Price (₹) *
              </label>
              <input
                type="number"
                step="any"
                {...register('sellingPrice')}
                placeholder="e.g. 475"
                className="w-full px-3 py-2 text-sm bg-white border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-600"
              />
              {errors.sellingPrice && (
                <p className="text-xs text-rose-600 mt-1">{errors.sellingPrice.message}</p>
              )}
            </div>

            <div>
              <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1">
                {isEditing ? 'Current Stock' : 'Initial Stock Quantity'}
              </label>
              <input
                type="number"
                {...register('currentStock')}
                disabled={isEditing}
                placeholder="e.g. 15"
                className="w-full px-3 py-2 text-sm bg-white border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-600 disabled:bg-slate-100 disabled:text-slate-500"
              />
              {isEditing && (
                <span className="text-[11px] text-slate-400">Use Stock Adjustment to update count</span>
              )}
              {errors.currentStock && (
                <p className="text-xs text-rose-600 mt-1">{errors.currentStock.message}</p>
              )}
            </div>

            <div>
              <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1">
                Minimum Stock Alert Threshold *
              </label>
              <input
                type="number"
                {...register('minStock')}
                placeholder="e.g. 10"
                className="w-full px-3 py-2 text-sm bg-white border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-600"
              />
              {errors.minStock && (
                <p className="text-xs text-rose-600 mt-1">{errors.minStock.message}</p>
              )}
            </div>

            <div>
              <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1">
                Unit of Measure *
              </label>
              <select
                {...register('unit')}
                className="w-full px-3 py-2 text-sm bg-white border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-600"
              >
                <option value={UnitType.Piece}>Piece (पीस)</option>
                <option value={UnitType.Kg}>Kg (किलो)</option>
                <option value={UnitType.Gram}>Gram (ग्राम)</option>
                <option value={UnitType.Litre}>Litre (लीटर)</option>
                <option value={UnitType.Box}>Box (पेटी/बॉक्स)</option>
                <option value={UnitType.Packet}>Packet (पैकेट)</option>
              </select>
            </div>

            <div>
              <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1">
                Status
              </label>
              <select
                {...register('status')}
                className="w-full px-3 py-2 text-sm bg-white border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-600"
              >
                <option value="Active">Active</option>
                <option value="Inactive">Inactive</option>
              </select>
            </div>

            <div className="sm:col-span-2">
              <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1">
                Description / Notes
              </label>
              <textarea
                rows={2}
                {...register('description')}
                placeholder="Optional supplier notes, brand details..."
                className="w-full px-3 py-2 text-sm bg-white border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-600"
              />
            </div>
          </div>

          <div className="mt-6 pt-4 border-t border-slate-100 flex items-center justify-end gap-3">
            <button
              type="button"
              onClick={onClose}
              disabled={isLoading}
              className="px-4 py-2 text-sm font-medium text-slate-700 bg-white border border-slate-200 rounded-lg hover:bg-slate-50 transition"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={isLoading}
              className="px-5 py-2 text-sm font-semibold text-white bg-indigo-600 hover:bg-indigo-700 rounded-lg shadow-sm transition flex items-center gap-2 disabled:opacity-50"
            >
              {isLoading ? (
                <>
                  <Loader2 className="w-4 h-4 animate-spin" /> Saving...
                </>
              ) : isEditing ? (
                'Save Changes'
              ) : (
                'Add Product'
              )}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

export default ProductFormModal;
