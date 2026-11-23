import React, { useState, useMemo } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Plus, Edit2, Trash2, Users, Phone, Mail, MapPin, X, Loader2 } from 'lucide-react';
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
  const pageSize = 6;

  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingSupplier, setEditingSupplier] = useState(null);
  const [deleteId, setDeleteId] = useState(null);

  const { data: suppliers = [], isLoading } = useQuery({
    queryKey: ['suppliers'],
    queryFn: () => supplierService.getSuppliers(),
  });

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm({
    resolver: zodResolver(supplierSchema),
    defaultValues: {
      name: '',
      contactPerson: '',
      phone: '',
      email: '',
      address: '',
      gstin: '',
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
      toast.success('Supplier removed');
      setDeleteId(null);
    },
    onError: (err) => toast.error(err.message || 'Failed to remove supplier'),
  });

  const handleOpenAdd = () => {
    setEditingSupplier(null);
    reset({
      name: '',
      contactPerson: '',
      phone: '',
      email: '',
      address: '',
      gstin: '',
      status: 'Active',
    });
    setIsModalOpen(true);
  };

  const handleOpenEdit = (sup) => {
    setEditingSupplier(sup);
    reset({
      name: sup.name,
      contactPerson: sup.contactPerson,
      phone: sup.phone,
      email: sup.email || '',
      address: sup.address,
      gstin: sup.gstin || '',
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
    return suppliers.filter((s) => {
      return (
        s.name.toLowerCase().includes(search.toLowerCase()) ||
        s.contactPerson.toLowerCase().includes(search.toLowerCase()) ||
        s.phone.includes(search)
      );
    });
  }, [suppliers, search]);

  const totalPages = Math.ceil(filtered.length / pageSize) || 1;
  const paginatedData = useMemo(() => {
    const start = (currentPage - 1) * pageSize;
    return filtered.slice(start, start + pageSize);
  }, [filtered, currentPage, pageSize]);

  const columns = [
    {
      header: 'Supplier / Wholesale Business',
      key: 'name',
      render: (r) => (
        <div className="flex items-center gap-2.5">
          <div className="p-2 bg-indigo-50 text-indigo-700 rounded-lg">
            <Users className="w-4 h-4" />
          </div>
          <div>
            <p className="font-semibold text-slate-900">{r.name}</p>
            {r.gstin && <p className="text-[11px] text-slate-400 font-mono">GST: {r.gstin}</p>}
          </div>
        </div>
      ),
    },
    {
      header: 'Contact Person',
      key: 'contactPerson',
      render: (r) => <span className="font-medium text-slate-800">{r.contactPerson}</span>,
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
      header: 'Location / Market',
      key: 'address',
      render: (r) => (
        <p className="text-xs text-slate-600 max-w-[220px] truncate" title={r.address}>
          {r.address}
        </p>
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
        title="Supplier Directory"
        subtitle="Manage relationships with Mandi grain wholesalers, edible oil distributors, and FMCG vendors."
        breadcrumbs={[{ label: 'Suppliers' }]}
        action={
          <button
            onClick={handleOpenAdd}
            className="inline-flex items-center gap-2 px-4 py-2 bg-indigo-600 hover:bg-indigo-700 text-white text-sm font-semibold rounded-lg shadow-sm transition"
          >
            <Plus className="w-4 h-4" />
            <span>Add Supplier</span>
          </button>
        }
      />

      <div className="bg-white p-4 rounded-xl border border-slate-200 flex flex-col sm:flex-row sm:items-center justify-between gap-3 shadow-xs">
        <SearchInput
          value={search}
          onChange={(val) => {
            setSearch(val);
            setCurrentPage(1);
          }}
          placeholder="Search by vendor name, person, or phone..."
        />
        <div className="text-xs text-slate-500 font-medium">
          Total Suppliers: <span className="font-semibold text-slate-900">{filtered.length}</span>
        </div>
      </div>

      <DataTable
        columns={columns}
        data={paginatedData}
        isLoading={isLoading}
        emptyTitle="No suppliers found"
        emptyDescription="Add wholesale distributors to streamline purchasing and AI reorder recommendations."
      />

      <Pagination
        currentPage={currentPage}
        totalPages={totalPages}
        onPageChange={setCurrentPage}
      />

      {/* Add / Edit modal */}
      {isModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/50 backdrop-blur-sm p-4 animate-in fade-in">
          <div className="bg-white rounded-2xl max-w-lg w-full p-6 shadow-xl border border-slate-100 relative">
            <button
              onClick={handleCloseModal}
              className="absolute top-4 right-4 text-slate-400 hover:text-slate-600"
            >
              <X className="w-5 h-5" />
            </button>
            <h3 className="text-lg font-bold text-slate-900 mb-1">
              {editingSupplier ? 'Edit Supplier' : 'Add New Wholesale Supplier'}
            </h3>
            <p className="text-xs text-slate-500 mb-4">
              Enter wholesaler contact info and address.
            </p>

            <form onSubmit={handleSubmit(onSubmit)} className="space-y-3.5">
              <div>
                <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1">
                  Business / Wholesaler Name *
                </label>
                <input
                  type="text"
                  {...register('name')}
                  placeholder="e.g. Lakshmi Wholesale Traders"
                  className="w-full px-3 py-2 text-sm bg-white border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-600"
                />
                {errors.name && <p className="text-xs text-rose-600 mt-1">{errors.name.message}</p>}
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
                    placeholder="orders@lakshmi.in"
                    className="w-full px-3 py-2 text-sm bg-white border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-600"
                  />
                  {errors.email && <p className="text-xs text-rose-600 mt-1">{errors.email.message}</p>}
                </div>

                <div>
                  <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1">
                    GSTIN (Optional)
                  </label>
                  <input
                    type="text"
                    {...register('gstin')}
                    placeholder="07AAAAA0000A1Z5"
                    className="w-full px-3 py-2 text-sm bg-white border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-600 font-mono"
                  />
                </div>
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1">
                  Mandi / Depot Address *
                </label>
                <input
                  type="text"
                  {...register('address')}
                  placeholder="Shop No, Mandi / Market, City"
                  className="w-full px-3 py-2 text-sm bg-white border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-600"
                />
                {errors.address && <p className="text-xs text-rose-600 mt-1">{errors.address.message}</p>}
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
        title="Remove Supplier"
        description="Are you sure you want to deactivate this supplier from your directory?"
        confirmText="Remove Supplier"
      />
    </div>
  );
}

export default SuppliersPage;

