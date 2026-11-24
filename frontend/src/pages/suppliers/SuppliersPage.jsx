import React, { useState, useMemo } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Link } from 'react-router-dom';
import {
  Plus,
  Edit2,
  Trash2,
  Users,
  Phone,
  Mail,
  MapPin,
  X,
  Loader2,
  Eye,
  FileSpreadsheet,
  Building2,
  CreditCard
} from 'lucide-react';
import { supplierSchema } from '../../validations/supplierSchemas';
import supplierService from '../../services/supplierService';
import PageHeader from '../../components/common/PageHeader';
import DataTable from '../../components/common/DataTable';
import SearchInput from '../../components/common/SearchInput';
import ConfirmDialog from '../../components/common/ConfirmDialog';
import Pagination from '../../components/common/Pagination';
import StatusBadge from '../../components/common/StatusBadge';
import { toast } from 'sonner';

export function SuppliersPage() {
  const queryClient = useQueryClient();

  const [search, setSearch] = useState('');
  const [currentPage, setCurrentPage] = useState(1);
  const pageSize = 8;

  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingSupplier, setEditingSupplier] = useState(null);
  const [deleteId, setDeleteId] = useState(null);

  const { data: suppliersData, isLoading } = useQuery({
    queryKey: ['suppliers'],
    queryFn: () => supplierService.getSuppliers(),
  });

  const suppliers = useMemo(() => {
    if (Array.isArray(suppliersData)) return suppliersData;
    if (suppliersData?.items && Array.isArray(suppliersData.items)) return suppliersData.items;
    return [];
  }, [suppliersData]);

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm({
    resolver: zodResolver(supplierSchema),
    defaultValues: {
      name: '',
      companyName: '',
      contactPerson: '',
      phone: '',
      email: '',
      address: '',
      billingAddress: '',
      gstin: '',
      panNumber: '',
      paymentTerms: 'Net 30 Days',
      notes: '',
      status: 'Active',
    },
  });

  const createMutation = useMutation({
    mutationFn: (data) => supplierService.createSupplier(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['suppliers'] });
      toast.success('Supplier added!');
      handleCloseModal();
    },
    onError: (err) => toast.error(err.message || 'Failed to add supplier'),
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }) => supplierService.updateSupplier(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['suppliers'] });
      toast.success('Supplier updated!');
      handleCloseModal();
    },
    onError: (err) => toast.error(err.message || 'Failed to update supplier'),
  });

  const deleteMutation = useMutation({
    mutationFn: (id) => supplierService.deleteSupplier(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['suppliers'] });
      toast.success('Supplier deactivated');
      setDeleteId(null);
    },
    onError: (err) => toast.error(err.message || 'Failed to deactivate supplier'),
  });

  const handleOpenAdd = () => {
    setEditingSupplier(null);
    reset({
      name: '',
      companyName: '',
      contactPerson: '',
      phone: '',
      email: '',
      address: '',
      billingAddress: '',
      gstin: '',
      panNumber: '',
      paymentTerms: 'Net 30 Days',
      notes: '',
      status: 'Active',
    });
    setIsModalOpen(true);
  };

  const handleOpenEdit = (sup) => {
    setEditingSupplier(sup);
    reset({
      name: sup.name || sup.supplierName,
      companyName: sup.companyName || '',
      contactPerson: sup.contactPerson || '',
      phone: sup.phone || '',
      email: sup.email || '',
      address: sup.address || '',
      billingAddress: sup.billingAddress || '',
      gstin: sup.gstin || sup.gstNumber || '',
      panNumber: sup.panNumber || '',
      paymentTerms: sup.paymentTerms || 'Net 30 Days',
      notes: sup.notes || '',
      status: sup.status || 'Active',
    });
    setIsModalOpen(true);
  };

  const handleCloseModal = () => {
    setIsModalOpen(false);
    setEditingSupplier(null);
  };

  const onSubmit = (formData) => {
    if (editingSupplier) {
      updateMutation.mutate({ id: editingSupplier.id, data: formData });
    } else {
      createMutation.mutate(formData);
    }
  };

  const filtered = useMemo(() => {
    const q = (search || '').toLowerCase().trim();
    if (!q) return suppliers;
    return suppliers.filter((s) => {
      const name = (s.name || s.supplierName || '').toLowerCase();
      const comp = (s.companyName || '').toLowerCase();
      const contact = (s.contactPerson || '').toLowerCase();
      const phone = String(s.phone || '');
      const gst = (s.gstin || s.gstNumber || '').toLowerCase();
      return name.includes(q) || comp.includes(q) || contact.includes(q) || phone.includes(q) || gst.includes(q);
    });
  }, [suppliers, search]);

  const totalPages = Math.ceil(filtered.length / pageSize) || 1;
  const paginatedData = useMemo(() => {
    const start = (currentPage - 1) * pageSize;
    return filtered.slice(start, start + pageSize);
  }, [filtered, currentPage, pageSize]);

  const columns = [
    {
      header: 'Supplier / Company',
      key: 'name',
      render: (r) => (
        <div className="flex items-center gap-2.5">
          <div className="p-2 bg-indigo-50 text-indigo-700 rounded-lg">
            <Building2 className="w-4 h-4" />
          </div>
          <div>
            <Link
              to={`/suppliers/${r.id}`}
              className="font-semibold text-slate-900 hover:text-indigo-600 transition"
            >
              {r.name}
            </Link>
            {r.companyName && <p className="text-xs text-slate-500">{r.companyName}</p>}
            {r.gstin && <p className="text-[11px] text-slate-400 font-mono">GST: {r.gstin}</p>}
          </div>
        </div>
      ),
    },
    {
      header: 'Contact Person',
      key: 'contactPerson',
      render: (r) => (
        <div>
          <span className="font-medium text-slate-800">{r.contactPerson}</span>
          {r.paymentTerms && <p className="text-[11px] text-slate-400">{r.paymentTerms}</p>}
        </div>
      ),
    },
    {
      header: 'Contact Info',
      key: 'phone',
      render: (r) => (
        <div className="space-y-0.5 text-xs text-slate-600">
          <p className="flex items-center gap-1.5">
            <Phone className="w-3 h-3 text-slate-400" />
            <span>{r.phone}</span>
          </p>
          {r.email && (
            <p className="flex items-center gap-1.5 text-slate-500">
              <Mail className="w-3 h-3 text-slate-400" />
              <span>{r.email}</span>
            </p>
          )}
        </div>
      ),
    },
    {
      header: 'Invoices',
      key: 'invoices',
      align: 'center',
      render: (r) => (
        <Link
          to={`/supplier-invoices?supplierId=${r.id}`}
          className="inline-flex items-center gap-1 px-2.5 py-1 text-xs font-semibold rounded-md bg-indigo-50 text-indigo-700 hover:bg-indigo-100 transition"
        >
          <FileSpreadsheet className="w-3 h-3" />
          <span>{r.invoicesCount ?? 0} Invoices</span>
        </Link>
      ),
    },
    {
      header: 'Status',
      key: 'status',
      align: 'center',
      render: (r) => <StatusBadge status={r.status || 'Active'} size="sm" />,
    },
    {
      header: 'Actions',
      key: 'actions',
      align: 'right',
      render: (r) => (
        <div className="flex items-center justify-end gap-1">
          <Link
            to={`/suppliers/${r.id}`}
            className="p-1.5 text-slate-500 hover:text-indigo-600 hover:bg-slate-100 rounded-md transition"
            title="View Supplier Profile & Purchases"
          >
            <Eye className="w-4 h-4" />
          </Link>
          <button
            onClick={() => handleOpenEdit(r)}
            className="p-1.5 text-slate-500 hover:text-indigo-600 hover:bg-slate-100 rounded-md transition"
            title="Edit Supplier"
          >
            <Edit2 className="w-4 h-4" />
          </button>
          <button
            onClick={() => setDeleteId(r.id)}
            className="p-1.5 text-slate-500 hover:text-rose-600 hover:bg-rose-50 rounded-md transition"
            title="Deactivate Supplier"
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
        title="Supplier Management"
        subtitle="Manage wholesale vendors, Mandi distributors, and direct invoice pipelines."
        breadcrumbs={[{ label: 'Suppliers' }]}
        action={
          <div className="flex items-center gap-2">
            <Link
              to="/supplier-invoices"
              className="inline-flex items-center gap-2 px-3.5 py-2 bg-indigo-50 text-indigo-700 hover:bg-indigo-100 text-sm font-semibold rounded-lg transition"
            >
              <FileSpreadsheet className="w-4 h-4" />
              <span>AI Invoices</span>
            </Link>
            <button
              onClick={handleOpenAdd}
              className="inline-flex items-center gap-2 px-4 py-2 bg-indigo-600 hover:bg-indigo-700 text-white text-sm font-semibold rounded-lg shadow-sm transition"
            >
              <Plus className="w-4 h-4" />
              <span>Add Supplier</span>
            </button>
          </div>
        }
      />

      <div className="bg-white p-4 rounded-xl border border-slate-200 flex flex-col sm:flex-row sm:items-center justify-between gap-3 shadow-xs">
        <div className="w-full sm:w-80">
          <SearchInput
            value={search}
            onChange={(e) => {
              setSearch(e.target.value);
              setCurrentPage(1);
            }}
            placeholder="Search by name, company, GSTIN or phone..."
          />
        </div>
        <div className="text-xs text-slate-500 font-medium">
          Showing {paginatedData.length} of {filtered.length} suppliers
        </div>
      </div>

      <DataTable
        columns={columns}
        data={paginatedData}
        isLoading={isLoading}
        emptyMessage="No suppliers registered yet. Click 'Add Supplier' to create your first vendor."
      />

      {totalPages > 1 && (
        <Pagination
          currentPage={currentPage}
          totalPages={totalPages}
          onPageChange={setCurrentPage}
        />
      )}

      {/* Add / Edit Supplier Modal */}
      {isModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/40 backdrop-blur-xs">
          <div className="bg-white rounded-2xl border border-slate-200 shadow-xl w-full max-w-xl overflow-hidden max-h-[90vh] flex flex-col">
            <div className="px-6 py-4 border-b border-slate-100 flex items-center justify-between bg-slate-50/50">
              <h3 className="font-semibold text-slate-900 text-base">
                {editingSupplier ? 'Edit Supplier Profile' : 'Add New Supplier'}
              </h3>
              <button
                onClick={handleCloseModal}
                className="p-1 text-slate-400 hover:text-slate-600 rounded-lg"
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            <form onSubmit={handleSubmit(onSubmit)} className="p-6 space-y-4 overflow-y-auto">
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1">
                    Supplier Name *
                  </label>
                  <input
                    type="text"
                    {...register('name')}
                    placeholder="e.g. Metro Wholesale"
                    className="w-full px-3 py-2 text-sm bg-white border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-600"
                  />
                  {errors.name && <p className="text-xs text-rose-600 mt-1">{errors.name.message}</p>}
                </div>

                <div>
                  <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1">
                    Company Legal Name
                  </label>
                  <input
                    type="text"
                    {...register('companyName')}
                    placeholder="e.g. Metro Cash & Carry India Pvt Ltd"
                    className="w-full px-3 py-2 text-sm bg-white border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-600"
                  />
                </div>
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1">
                    Contact Person *
                  </label>
                  <input
                    type="text"
                    {...register('contactPerson')}
                    placeholder="e.g. Ramesh Gupta"
                    className="w-full px-3 py-2 text-sm bg-white border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-600"
                  />
                  {errors.contactPerson && (
                    <p className="text-xs text-rose-600 mt-1">{errors.contactPerson.message}</p>
                  )}
                </div>

                <div>
                  <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1">
                    Phone Number *
                  </label>
                  <input
                    type="text"
                    {...register('phone')}
                    placeholder="9811234567"
                    className="w-full px-3 py-2 text-sm bg-white border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-600"
                  />
                  {errors.phone && <p className="text-xs text-rose-600 mt-1">{errors.phone.message}</p>}
                </div>
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1">
                    Email Address
                  </label>
                  <input
                    type="email"
                    {...register('email')}
                    placeholder="orders@metro.in"
                    className="w-full px-3 py-2 text-sm bg-white border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-600"
                  />
                  {errors.email && <p className="text-xs text-rose-600 mt-1">{errors.email.message}</p>}
                </div>

                <div>
                  <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1">
                    Payment Terms
                  </label>
                  <input
                    type="text"
                    {...register('paymentTerms')}
                    placeholder="e.g. Net 15 Days, Cash On Delivery"
                    className="w-full px-3 py-2 text-sm bg-white border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-600"
                  />
                </div>
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1">
                    GST Number (Optional)
                  </label>
                  <input
                    type="text"
                    {...register('gstin')}
                    placeholder="27AAAAA0000A1Z5"
                    className="w-full px-3 py-2 text-sm bg-white border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-600 font-mono uppercase"
                  />
                </div>

                <div>
                  <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1">
                    PAN Number (Optional)
                  </label>
                  <input
                    type="text"
                    {...register('panNumber')}
                    placeholder="AAAAA0000A"
                    className="w-full px-3 py-2 text-sm bg-white border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-600 font-mono uppercase"
                  />
                </div>
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1">
                  Supplier Physical Address *
                </label>
                <input
                  type="text"
                  {...register('address')}
                  placeholder="Shop No, Mandi / Market, City, State"
                  className="w-full px-3 py-2 text-sm bg-white border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-600"
                />
                {errors.address && <p className="text-xs text-rose-600 mt-1">{errors.address.message}</p>}
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1">
                  Billing Address (Optional)
                </label>
                <input
                  type="text"
                  {...register('billingAddress')}
                  placeholder="Registered corporate billing address"
                  className="w-full px-3 py-2 text-sm bg-white border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-600"
                />
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1">
                  Notes
                </label>
                <textarea
                  rows={2}
                  {...register('notes')}
                  placeholder="Delivery timing preferences, preferred payment modes..."
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
                  {editingSupplier ? 'Save Changes' : 'Add Supplier'}
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
        title="Deactivate Supplier"
        description="Are you sure you want to deactivate this supplier from your directory?"
        confirmText="Deactivate"
      />
    </div>
  );
}

export default SuppliersPage;
