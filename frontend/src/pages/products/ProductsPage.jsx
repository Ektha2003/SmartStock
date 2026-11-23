import React, { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Plus, Eye, Edit2, Trash2 } from 'lucide-react';
import productService from '../../services/productService';
import categoryService from '../../services/categoryService';
import PageHeader from '../../components/common/PageHeader';
import DataTable from '../../components/common/DataTable';
import SearchInput from '../../components/common/SearchInput';
import FilterDropdown from '../../components/common/FilterDropdown';
import StatusBadge from '../../components/common/StatusBadge';
import ConfirmDialog from '../../components/common/ConfirmDialog';
import ProductFormModal from '../../components/products/ProductFormModal';
import Pagination from '../../components/common/Pagination';
import { formatCurrency } from '../../utils/formatters';
import { toast } from 'sonner';

export function ProductsPage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const [search, setSearch] = useState('');
  const [selectedCategory, setSelectedCategory] = useState('');
  const [selectedStockStatus, setSelectedStockStatus] = useState('');
  const [currentPage, setCurrentPage] = useState(1);
  const pageSize = 8;

  const [editProduct, setEditProduct] = useState(null);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [deleteId, setDeleteId] = useState(null);

  const { data: productsData, isLoading } = useQuery({
    queryKey: ['products', currentPage, search, selectedCategory, selectedStockStatus],
    queryFn: () =>
      productService.getProducts({
        pageNumber: currentPage,
        pageSize,
        search: search || undefined,
        categoryId: selectedCategory || undefined,
        lowStockOnly: selectedStockStatus === 'low' ? true : undefined,
      }),
  });

  const { data: categories = [] } = useQuery({
    queryKey: ['categories'],
    queryFn: () => categoryService.getCategories(),
  });

  const products = productsData?.items || (Array.isArray(productsData) ? productsData : []);
  const totalCount = productsData?.totalCount || products.length;
  const totalPages = Math.ceil(totalCount / pageSize) || 1;

  const updateMutation = useMutation({
    mutationFn: ({ id, data }) => productService.updateProduct(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['products'] });
      queryClient.invalidateQueries({ queryKey: ['dashboard'] });
      toast.success('Product updated successfully!');
      setIsModalOpen(false);
      setEditProduct(null);
    },
    onError: (err) => {
      toast.error(err.message || 'Failed to update product');
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (id) => productService.deleteProduct(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['products'] });
      queryClient.invalidateQueries({ queryKey: ['dashboard'] });
      toast.success('Product removed from catalog');
      setDeleteId(null);
    },
    onError: (err) => {
      toast.error(err.message || 'Failed to delete product');
    },
  });

  const columns = [
    {
      header: 'Product',
      key: 'name',
      render: (row) => (
        <div>
          <p className="font-semibold text-slate-900">{row.name}</p>
          <span className="text-[11px] text-slate-400">{row.description || 'Kirana item'}</span>
        </div>
      ),
    },
    {
      header: 'SKU / Barcode',
      key: 'sku',
      render: (row) => (
        <span className="font-mono text-xs text-slate-600 bg-slate-100 px-2 py-0.5 rounded">
          {row.sku}
        </span>
      ),
    },
    {
      header: 'Category',
      key: 'categoryName',
      render: (row) => <span className="text-slate-600 font-medium">{row.categoryName || '-'}</span>,
    },
    {
      header: 'Cost Price',
      key: 'purchasePrice',
      align: 'right',
      render: (row) => <span className="text-slate-600">{formatCurrency(row.purchasePrice)}</span>,
    },
    {
      header: 'Selling Price',
      key: 'sellingPrice',
      align: 'right',
      render: (row) => (
        <span className="font-bold text-slate-900">{formatCurrency(row.sellingPrice)}</span>
      ),
    },
    {
      header: 'Stock',
      key: 'currentStock',
      align: 'center',
      render: (row) => (
        <span
          className={`font-semibold ${
            row.currentStock === 0
              ? 'text-rose-600'
              : row.currentStock <= (row.minimumStock ?? row.minStock)
              ? 'text-amber-600'
              : 'text-slate-900'
          }`}
        >
          {row.currentStock} {row.unitName || ''}
        </span>
      ),
    },
    {
      header: 'Status',
      key: 'status',
      align: 'center',
      render: (row) => {
        let status = 'In Stock';
        if (row.currentStock === 0) status = 'Out of Stock';
        else if (row.currentStock <= (row.minimumStock ?? row.minStock)) status = 'Low Stock';
        return <StatusBadge status={status} size="sm" />;
      },
    },
    {
      header: 'Actions',
      key: 'actions',
      align: 'right',
      render: (row) => (
        <div className="flex items-center justify-end gap-1" onClick={(e) => e.stopPropagation()}>
          <button
            onClick={() => navigate(`/products/${row.id}`)}
            className="p-1.5 text-slate-500 hover:text-indigo-600 hover:bg-slate-100 rounded-md transition"
            title="View Details"
          >
            <Eye className="w-4 h-4" />
          </button>
          <button
            onClick={() => {
              setEditProduct(row);
              setIsModalOpen(true);
            }}
            className="p-1.5 text-slate-500 hover:text-indigo-600 hover:bg-slate-100 rounded-md transition"
            title="Edit Product"
          >
            <Edit2 className="w-4 h-4" />
          </button>
          <button
            onClick={() => setDeleteId(row.id)}
            className="p-1.5 text-slate-500 hover:text-rose-600 hover:bg-rose-50 rounded-md transition"
            title="Deactivate / Delete"
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
        title="Products Management"
        subtitle="View, search, and manage your Kirana grocery catalog and pricing."
        breadcrumbs={[{ label: 'Products' }]}
        action={
          <Link
            to="/products/new"
            className="inline-flex items-center gap-2 px-4 py-2 bg-indigo-600 hover:bg-indigo-700 text-white text-sm font-semibold rounded-lg shadow-sm transition"
          >
            <Plus className="w-4 h-4" />
            <span>Add Product</span>
          </Link>
        }
      />

      {/* Filters bar */}
      <div className="bg-white p-4 rounded-xl border border-slate-200 flex flex-col sm:flex-row sm:items-center justify-between gap-3 shadow-xs">
        <div className="flex flex-col sm:flex-row sm:items-center gap-3 flex-1">
          <SearchInput
            value={search}
            onChange={(val) => {
              setSearch(val);
              setCurrentPage(1);
            }}
            placeholder="Search by product name or SKU..."
          />
          <FilterDropdown
            label="All Categories"
            value={selectedCategory}
            options={categories.map((c) => ({ label: c.name, value: c.id }))}
            onChange={(val) => {
              setSelectedCategory(val);
              setCurrentPage(1);
            }}
          />
          <FilterDropdown
            label="All Stock Status"
            value={selectedStockStatus}
            options={[
              { label: 'In Stock (> Min)', value: 'in' },
              { label: 'Low Stock (<= Min)', value: 'low' },
              { label: 'Out of Stock (0)', value: 'out' },
            ]}
            onChange={(val) => {
              setSelectedStockStatus(val);
              setCurrentPage(1);
            }}
          />
        </div>

        <div className="text-xs text-slate-500 font-medium">
          Total Products: <span className="font-semibold text-slate-900">{totalCount}</span>
        </div>
      </div>

      {/* Table */}
      <DataTable
        columns={columns}
        data={products}
        isLoading={isLoading}
        onRowClick={(row) => navigate(`/products/${row.id}`)}
        emptyTitle="No products match your search"
        emptyDescription="Try clearing your category or stock filter to see all items."
      />

      <Pagination
        currentPage={currentPage}
        totalPages={totalPages}
        onPageChange={setCurrentPage}
      />

      {/* Edit modal */}
      {isModalOpen && (
        <ProductFormModal
          isOpen={isModalOpen}
          onClose={() => {
            setIsModalOpen(false);
            setEditProduct(null);
          }}
          initialData={editProduct}
          categories={categories}
          isLoading={updateMutation.isPending}
          onSubmit={(data) => updateMutation.mutate({ id: editProduct.id, data })}
        />
      )}

      {/* Delete confirmation */}
      <ConfirmDialog
        isOpen={!!deleteId}
        onClose={() => setDeleteId(null)}
        onConfirm={() => deleteMutation.mutate(deleteId)}
        isLoading={deleteMutation.isPending}
        title="Delete Product"
        description="Are you sure you want to remove this product from your catalog? Existing sales and purchase records will be preserved."
        confirmText="Remove Product"
      />
    </div>
  );
}

export default ProductsPage;
