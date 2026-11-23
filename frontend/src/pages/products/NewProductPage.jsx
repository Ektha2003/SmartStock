import React from 'react';
import { useNavigate } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { productSchema } from '../../validations/productSchemas';
import { UnitType } from '../../utils/constants';
import productService from '../../services/productService';
import categoryService from '../../services/categoryService';
import PageHeader from '../../components/common/PageHeader';
import { Loader2, ArrowLeft } from 'lucide-react';
import { toast } from 'sonner';

export function NewProductPage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const { data: categories = [] } = useQuery({
    queryKey: ['categories'],
    queryFn: () => categoryService.getCategories(),
  });

  const createMutation = useMutation({
    mutationFn: (data) => productService.createProduct(data),
    onSuccess: (newProduct) => {
      queryClient.invalidateQueries({ queryKey: ['products'] });
      queryClient.invalidateQueries({ queryKey: ['dashboard'] });
      queryClient.invalidateQueries({ queryKey: ['inventory'] });
      toast.success(`Product "${newProduct.name}" added to catalog!`);
      navigate('/products');
    },
    onError: (err) => {
      toast.error(err.message || 'Failed to create product');
    },
  });

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm({
    resolver: zodResolver(productSchema),
    defaultValues: {
      name: '',
      sku: '',
      categoryId: '',
      description: '',
      purchasePrice: '',
      sellingPrice: '',
      currentStock: 0,
      minStock: 5,
      unit: 1,
      status: 'Active',
    },
  });

  const onSubmit = (formData) => {
    createMutation.mutate(formData);
  };

  return (
    <div className="max-w-3xl mx-auto space-y-6">
      <PageHeader
        title="Add New Product"
        subtitle="Register a new item into your Kirana store inventory."
        breadcrumbs={[
          { label: 'Products', to: '/products' },
          { label: 'Add New' },
        ]}
        action={
          <button
            onClick={() => navigate('/products')}
            className="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold text-slate-700 bg-white border border-slate-200 rounded-lg hover:bg-slate-50 transition"
          >
            <ArrowLeft className="w-3.5 h-3.5" /> Back to Products
          </button>
        }
      />

      <div className="bg-white rounded-2xl border border-slate-200 p-6 sm:p-8 shadow-sm">
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-5">
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
                SKU / Barcode ID *
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
                Initial Stock on Shelf
              </label>
              <input
                type="number"
                {...register('currentStock')}
                placeholder="e.g. 15"
                className="w-full px-3 py-2 text-sm bg-white border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-600"
              />
              {errors.currentStock && (
                <p className="text-xs text-rose-600 mt-1">{errors.currentStock.message}</p>
              )}
            </div>

            <div>
              <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1">
                Min Stock Alert Level *
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
                Description / Details
              </label>
              <textarea
                rows={2}
                {...register('description')}
                placeholder="Product attributes, brand, packaging info..."
                className="w-full px-3 py-2 text-sm bg-white border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-600"
              />
            </div>
          </div>

          <div className="pt-4 border-t border-slate-100 flex items-center justify-end gap-3">
            <button
              type="button"
              onClick={() => navigate('/products')}
              className="px-4 py-2 text-sm font-medium text-slate-700 bg-white border border-slate-200 rounded-lg hover:bg-slate-50 transition"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={createMutation.isPending}
              className="px-6 py-2 bg-indigo-600 hover:bg-indigo-700 text-white text-sm font-semibold rounded-lg shadow-sm transition flex items-center gap-2 disabled:opacity-50"
            >
              {createMutation.isPending ? (
                <>
                  <Loader2 className="w-4 h-4 animate-spin" /> Saving...
                </>
              ) : (
                'Save Product'
              )}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

export default NewProductPage;
