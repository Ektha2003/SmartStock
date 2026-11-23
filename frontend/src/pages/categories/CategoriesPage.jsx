import React, { useState, useMemo } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Plus, Edit2, Trash2, FolderTree, Package, X, Loader2 } from 'lucide-react';
import categoryService from '../../services/categoryService';
import PageHeader from '../../components/common/PageHeader';
import DataTable from '../../components/common/DataTable';
import ConfirmDialog from '../../components/common/ConfirmDialog';
import { toast } from 'sonner';

export function CategoriesPage() {
  const queryClient = useQueryClient();

  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingCategory, setEditingCategory] = useState(null);
  const [categoryName, setCategoryName] = useState('');
  const [categoryDesc, setCategoryDesc] = useState('');
  const [deleteId, setDeleteId] = useState(null);

  const { data: categoriesData, isLoading } = useQuery({
    queryKey: ['categories'],
    queryFn: () => categoryService.getCategories(),
  });

  const categories = useMemo(() => {
    if (Array.isArray(categoriesData)) return categoriesData;
    if (categoriesData?.items && Array.isArray(categoriesData.items)) return categoriesData.items;
    return [];
  }, [categoriesData]);

  const createMutation = useMutation({
    mutationFn: (data) => categoryService.createCategory(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['categories'] });
      toast.success('Category created!');
      handleCloseModal();
    },
    onError: (err) => toast.error(err.message || 'Failed to create category'),
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }) => categoryService.updateCategory(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['categories'] });
      toast.success('Category updated!');
      handleCloseModal();
    },
    onError: (err) => toast.error(err.message || 'Failed to update category'),
  });

  const deleteMutation = useMutation({
    mutationFn: (id) => categoryService.deleteCategory(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['categories'] });
      toast.success('Category deleted');
      setDeleteId(null);
    },
    onError: (err) => toast.error(err.message || 'Failed to delete category'),
  });

  const handleOpenAdd = () => {
    setEditingCategory(null);
    setCategoryName('');
    setCategoryDesc('');
    setIsModalOpen(true);
  };

  const handleOpenEdit = (cat) => {
    setEditingCategory(cat);
    setCategoryName(cat.name);
    setCategoryDesc(cat.description || '');
    setIsModalOpen(true);
  };

  const handleCloseModal = () => {
    setIsModalOpen(false);
    setEditingCategory(null);
    setCategoryName('');
    setCategoryDesc('');
  };

  const handleSubmit = (e) => {
    e.preventDefault();
    if (!categoryName.trim()) {
      toast.error('Category name is required');
      return;
    }
    if (editingCategory) {
      updateMutation.mutate({
        id: editingCategory.id,
        data: { name: categoryName.trim(), description: categoryDesc.trim() },
      });
    } else {
      createMutation.mutate({
        name: categoryName.trim(),
        description: categoryDesc.trim(),
      });
    }
  };

  const columns = [
    {
      header: 'Category Name',
      key: 'name',
      render: (r) => (
        <div className="flex items-center gap-2.5">
          <div className="p-2 bg-indigo-50 text-indigo-700 rounded-lg">
            <FolderTree className="w-4 h-4" />
          </div>
          <div>
            <p className="font-semibold text-slate-900">{r.name}</p>
            <p className="text-[11px] text-slate-500">{r.description || 'Kirana category'}</p>
          </div>
        </div>
      ),
    },
    {
      header: 'Products Count',
      key: 'productCount',
      align: 'center',
      render: (r) => (
        <span className="inline-flex items-center gap-1.5 px-2.5 py-1 bg-slate-100 text-slate-700 text-xs font-semibold rounded-full">
          <Package className="w-3.5 h-3.5 text-slate-500" />
          {r.productCount ?? 0} SKUs
        </span>
      ),
    },
    {
      header: 'Actions',
      key: 'actions',
      align: 'right',
      render: (r) => (
        <div className="flex items-center justify-end gap-1">
          <button
            onClick={() => handleOpenEdit(r)}
            className="p-1.5 text-slate-500 hover:text-indigo-600 hover:bg-slate-100 rounded-md transition"
            title="Edit Category"
          >
            <Edit2 className="w-4 h-4" />
          </button>
          <button
            onClick={() => setDeleteId(r.id)}
            className="p-1.5 text-slate-500 hover:text-rose-600 hover:bg-rose-50 rounded-md transition"
            title="Delete Category"
          >
            <Trash2 className="w-4 h-4" />
          </button>
        </div>
      ),
    },
  ];

  return (
    <div className="space-y-5">
      <PageHeader
        title="Category Management"
        subtitle="Organize items by aisles and departments (Staples, Dairy, Beverages, Snacks, etc.)"
        breadcrumbs={[{ label: 'Categories' }]}
        action={
          <button
            onClick={handleOpenAdd}
            className="inline-flex items-center gap-2 px-4 py-2 bg-indigo-600 hover:bg-indigo-700 text-white text-sm font-semibold rounded-lg shadow-sm transition"
          >
            <Plus className="w-4 h-4" />
            <span>Add Category</span>
          </button>
        }
      />

      <DataTable
        columns={columns}
        data={categories}
        isLoading={isLoading}
        emptyTitle="No categories yet"
        emptyDescription="Create your first Kirana category to organize products."
      />

      {/* Add / Edit modal */}
      {isModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
          {/* Backdrop with negative inset to push Chromium blur edge bleeding off-screen */}
          <div
            onClick={handleCloseModal}
            className="fixed -inset-10 bg-slate-900/50 backdrop-blur-sm"
          />
          <div className="bg-white rounded-2xl max-w-md w-full p-6 shadow-xl border border-slate-100 relative z-10 animate-in fade-in">
            <button
              onClick={handleCloseModal}
              className="absolute top-4 right-4 text-slate-400 hover:text-slate-600"
            >
              <X className="w-5 h-5" />
            </button>
            <h3 className="text-lg font-bold text-slate-900 mb-1">
              {editingCategory ? 'Edit Category' : 'Add New Category'}
            </h3>
            <p className="text-xs text-slate-500 mb-4">
              {editingCategory ? 'Update category name or description' : 'Enter the category details below'}
            </p>

            <form onSubmit={handleSubmit} className="space-y-4">
              <div>
                <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1">
                  Category Name *
                </label>
                <input
                  type="text"
                  value={categoryName}
                  onChange={(e) => setCategoryName(e.target.value)}
                  placeholder="e.g. Edible Oils & Ghee"
                  className="w-full px-3 py-2 text-sm bg-white border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-600"
                />
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1">
                  Description / Department
                </label>
                <textarea
                  rows={2}
                  value={categoryDesc}
                  onChange={(e) => setCategoryDesc(e.target.value)}
                  placeholder="e.g. Mustard oil, Refined oil, Vanaspati, Desi ghee"
                  className="w-full px-3 py-2 text-sm bg-white border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-600"
                />
              </div>

              <div className="pt-3 border-t border-slate-100 flex items-center justify-end gap-2">
                <button
                  type="button"
                  onClick={handleCloseModal}
                  className="px-4 py-2 text-sm font-medium text-slate-700 bg-white border border-slate-200 rounded-lg hover:bg-slate-50"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={createMutation.isPending || updateMutation.isPending}
                  className="px-5 py-2 text-sm font-semibold text-white bg-indigo-600 hover:bg-indigo-700 rounded-lg shadow-sm flex items-center gap-2"
                >
                  {(createMutation.isPending || updateMutation.isPending) && (
                    <Loader2 className="w-4 h-4 animate-spin" />
                  )}
                  {editingCategory ? 'Save Changes' : 'Create Category'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Delete confirmation dialog */}
      <ConfirmDialog
        isOpen={!!deleteId}
        onClose={() => setDeleteId(null)}
        onConfirm={() => deleteMutation.mutate(deleteId)}
        isLoading={deleteMutation.isPending}
        title="Delete Category"
        description="Are you sure you want to delete this category? Associated products will have their category unlinked."
        confirmText="Delete Category"
      />
    </div>
  );
}

export default CategoriesPage;

