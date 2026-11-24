import React, { useState, useMemo } from 'react';
import { createPortal } from 'react-dom';
import { useQuery, useMutation, useQueryClient, keepPreviousData } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
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
  Repeat,
  CheckCircle2,
  XCircle,
  Eye,
  Calendar,
  MessageSquare,
} from 'lucide-react';
import customerService from '../../services/customerService';
import PageHeader from '../../components/common/PageHeader';
import DataTable from '../../components/common/DataTable';
import SearchInput from '../../components/common/SearchInput';
import ConfirmDialog from '../../components/common/ConfirmDialog';
import StatusBadge from '../../components/common/StatusBadge';
import { toast } from 'sonner';

export default function CustomersPage() {
  const queryClient = useQueryClient();
  const navigate = useNavigate();

  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState('ALL'); // ALL, ACTIVE, INACTIVE
  const [recurringOnly, setRecurringOnly] = useState(false);
  const [currentPage, setCurrentPage] = useState(1);
  const pageSize = 10;

  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingCustomer, setEditingCustomer] = useState(null);
  const [deleteId, setDeleteId] = useState(null);

  // Form State
  const [formData, setFormData] = useState({
    name: '',
    email: '',
    phone: '',
    billingAddress: '',
    deliveryAddress: '',
    preferredContactMethod: 1, // Email
    recurringOrderEnabled: true,
    defaultDeliveryInstructions: '',
    notes: '',
  });

  const { data: customersData, isLoading } = useQuery({
    queryKey: ['customers', search, statusFilter, recurringOnly, currentPage],
    queryFn: () =>
      customerService.getCustomers({
        search: search || undefined,
        isActive: statusFilter === 'ACTIVE' ? true : statusFilter === 'INACTIVE' ? false : undefined,
        recurringOnly: recurringOnly ? true : undefined,
        pageNumber: currentPage,
        pageSize,
      }),
    placeholderData: keepPreviousData,
  });

  const customers = useMemo(() => {
    if (Array.isArray(customersData)) return customersData;
    if (customersData?.items && Array.isArray(customersData.items)) return customersData.items;
    return [];
  }, [customersData]);

  const totalPages = customersData?.totalPages || 1;
  const totalCount = customersData?.totalCount || customers.length;

  const createMutation = useMutation({
    mutationFn: (data) => customerService.createCustomer(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['customers'] });
      toast.success('Customer registered successfully!');
      handleCloseModal();
    },
    onError: (err) => {
      toast.error(err.message || 'Failed to create customer');
    },
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }) => customerService.updateCustomer(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['customers'] });
      toast.success('Customer details updated!');
      handleCloseModal();
    },
    onError: (err) => {
      toast.error(err.message || 'Failed to update customer');
    },
  });

  const toggleStatusMutation = useMutation({
    mutationFn: (id) => customerService.toggleCustomerStatus(id),
    onSuccess: (res) => {
      queryClient.invalidateQueries({ queryKey: ['customers'] });
      toast.success('Customer status updated');
    },
    onError: (err) => {
      toast.error(err.message || 'Failed to toggle status');
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (id) => customerService.deleteCustomer(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['customers'] });
      toast.success('Customer removed or deactivated.');
      setDeleteId(null);
    },
    onError: (err) => {
      toast.error(err.message || 'Failed to delete customer');
    },
  });

  const handleOpenAdd = () => {
    setEditingCustomer(null);
    setFormData({
      name: '',
      email: '',
      phone: '',
      billingAddress: '',
      deliveryAddress: '',
      preferredContactMethod: 1,
      recurringOrderEnabled: true,
      defaultDeliveryInstructions: '',
      notes: '',
    });
    setIsModalOpen(true);
  };

  const handleOpenEdit = (customer) => {
    setEditingCustomer(customer);
    setFormData({
      name: customer.name || '',
      email: customer.email || '',
      phone: customer.phone || '',
      billingAddress: customer.billingAddress || '',
      deliveryAddress: customer.deliveryAddress || '',
      preferredContactMethod: customer.preferredContactMethod || 1,
      recurringOrderEnabled: !!customer.recurringOrderEnabled,
      defaultDeliveryInstructions: customer.defaultDeliveryInstructions || '',
      notes: customer.notes || '',
    });
    setIsModalOpen(true);
  };

  const handleCloseModal = () => {
    setIsModalOpen(false);
    setEditingCustomer(null);
  };

  const handleSubmit = (e) => {
    e.preventDefault();
    if (!formData.name.trim() || !formData.email.trim() || !formData.deliveryAddress.trim()) {
      toast.error('Name, email, and delivery address are required.');
      return;
    }

    if (editingCustomer) {
      updateMutation.mutate({
        id: editingCustomer.id,
        data: {
          ...formData,
          isActive: editingCustomer.isActive,
        },
      });
    } else {
      createMutation.mutate(formData);
    }
  };

  const columns = [
    {
      header: 'Customer',
      key: 'name',
      render: (row) => (
        <div className="flex items-center gap-3">
          <div className="h-10 w-10 rounded-full bg-blue-100 dark:bg-blue-900/40 text-blue-600 dark:text-blue-400 font-semibold flex items-center justify-center">
            {row.name.substring(0, 2).toUpperCase()}
          </div>
          <div>
            <button
              onClick={() => navigate(`/customers/${row.id}`)}
              className="text-left font-semibold text-slate-900 dark:text-slate-100 hover:text-blue-600 transition"
            >
              {row.name}
            </button>
            <div className="text-xs text-slate-500 flex items-center gap-2 mt-0.5">
              <span className="flex items-center gap-1">
                <Mail className="w-3 h-3 text-slate-400" />
                {row.email}
              </span>
              <span>•</span>
              <span className="flex items-center gap-1">
                <Phone className="w-3 h-3 text-slate-400" />
                {row.phone}
              </span>
            </div>
          </div>
        </div>
      ),
    },
    {
      header: 'Delivery Address',
      key: 'deliveryAddress',
      render: (row) => (
        <div className="flex items-start gap-1.5 max-w-xs text-xs text-slate-600 dark:text-slate-300">
          <MapPin className="w-3.5 h-3.5 text-slate-400 shrink-0 mt-0.5" />
          <span className="truncate">{row.deliveryAddress || 'Not specified'}</span>
        </div>
      ),
    },
    {
      header: 'Subscription',
      key: 'recurringOrderEnabled',
      render: (row) => (
        <div>
          {row.recurringOrderEnabled ? (
            <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-medium bg-emerald-100 text-emerald-800 dark:bg-emerald-900/40 dark:text-emerald-300">
              <Repeat className="w-3 h-3" />
              {row.recurringSetting?.frequencyTypeName || 'Active'}
            </span>
          ) : (
            <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-medium bg-slate-100 text-slate-600 dark:bg-slate-800 dark:text-slate-400">
              Disabled
            </span>
          )}
          {row.nextOrderDate && (
            <div className="text-[11px] text-slate-500 mt-1 flex items-center gap-1">
              <Calendar className="w-3 h-3 text-slate-400" />
              Next: {new Date(row.nextOrderDate).toLocaleDateString()}
            </div>
          )}
        </div>
      ),
    },
    {
      header: 'Products',
      key: 'recurringProductsCount',
      render: (row) => (
        <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-slate-100 dark:bg-slate-800 text-slate-700 dark:text-slate-300">
          {row.recurringProductsCount || 0} items
        </span>
      ),
    },
    {
      header: 'Status',
      key: 'isActive',
      render: (row) => (
        <StatusBadge status={row.isActive ? 'Active' : 'Inactive'} />
      ),
    },
    {
      header: 'Actions',
      key: 'actions',
      render: (row) => (
        <div className="flex items-center gap-1.5">
          <button
            onClick={() => navigate(`/customers/${row.id}`)}
            className="p-1.5 text-slate-500 hover:text-blue-600 hover:bg-blue-50 dark:hover:bg-blue-900/30 rounded transition"
            title="View Details & Recurring Products"
          >
            <Eye className="w-4 h-4" />
          </button>
          <button
            onClick={() => handleOpenEdit(row)}
            className="p-1.5 text-slate-500 hover:text-amber-600 hover:bg-amber-50 dark:hover:bg-amber-900/30 rounded transition"
            title="Edit Customer"
          >
            <Edit2 className="w-4 h-4" />
          </button>
          <button
            onClick={() => toggleStatusMutation.mutate(row.id)}
            className={`p-1.5 rounded transition ${
              row.isActive
                ? 'text-slate-500 hover:text-rose-600 hover:bg-rose-50'
                : 'text-slate-500 hover:text-emerald-600 hover:bg-emerald-50'
            }`}
            title={row.isActive ? 'Deactivate Customer' : 'Activate Customer'}
          >
            {row.isActive ? <XCircle className="w-4 h-4" /> : <CheckCircle2 className="w-4 h-4" />}
          </button>
          <button
            onClick={() => setDeleteId(row.id)}
            className="p-1.5 text-slate-500 hover:text-red-600 hover:bg-red-50 dark:hover:bg-red-900/30 rounded transition"
            title="Delete Customer"
          >
            <Trash2 className="w-4 h-4" />
          </button>
        </div>
      ),
    },
  ];

  return (
    <div className="space-y-6">
      <PageHeader
        title="Customer Management"
        subtitle="Manage fixed/regular customers and recurring grocery subscription profiles."
      >
        <button
          onClick={handleOpenAdd}
          className="flex items-center gap-2 px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg font-medium shadow-sm transition"
        >
          <Plus className="w-4 h-4" />
          Add Customer
        </button>
      </PageHeader>

      {/* Filter and Search Bar */}
      <div className="bg-white dark:bg-slate-800 p-4 rounded-xl border border-slate-200 dark:border-slate-700 shadow-sm flex flex-col md:flex-row items-center justify-between gap-4">
        <div className="w-full md:w-80">
          <SearchInput
            value={search}
            onChange={(val) => {
              setSearch(val);
              setCurrentPage(1);
            }}
            placeholder="Search by name, email, phone..."
          />
        </div>

        <div className="flex flex-wrap items-center gap-3 w-full md:w-auto">
          {/* Status selector */}
          <select
            value={statusFilter}
            onChange={(e) => {
              setStatusFilter(e.target.value);
              setCurrentPage(1);
            }}
            className="px-3 py-2 text-sm border border-slate-200 dark:border-slate-700 rounded-lg bg-white dark:bg-slate-900 text-slate-800 dark:text-slate-200"
          >
            <option value="ALL">All Statuses</option>
            <option value="ACTIVE">Active Only</option>
            <option value="INACTIVE">Inactive Only</option>
          </select>

          {/* Recurring Orders Toggle */}
          <button
            onClick={() => {
              setRecurringOnly(!recurringOnly);
              setCurrentPage(1);
            }}
            className={`flex items-center gap-1.5 px-3 py-2 text-sm rounded-lg border transition ${
              recurringOnly
                ? 'bg-blue-50 border-blue-300 text-blue-700 dark:bg-blue-900/30 dark:border-blue-700 dark:text-blue-300 font-medium'
                : 'border-slate-200 dark:border-slate-700 text-slate-600 dark:text-slate-400 bg-white dark:bg-slate-900'
            }`}
          >
            <Repeat className="w-4 h-4" />
            Recurring Only
          </button>
        </div>
      </div>

      {/* Customer Table */}
      <DataTable
        columns={columns}
        data={customers}
        isLoading={isLoading}
        emptyTitle="No customers found"
        emptyDescription="No customers match your current search or filters."
        pagination={{ currentPage, totalPages, onPageChange: setCurrentPage, totalCount, pageSize }}
      />

      {/* Add / Edit Customer Modal */}
      {isModalOpen && createPortal(
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/50 backdrop-blur-sm">
          <div className="bg-white dark:bg-slate-800 rounded-2xl max-w-xl w-full p-6 shadow-xl border border-slate-200 dark:border-slate-700 max-h-[90vh] overflow-y-auto">
            <div className="flex items-center justify-between pb-4 border-b border-slate-200 dark:border-slate-700 mb-5">
              <h3 className="text-lg font-bold text-slate-900 dark:text-slate-100 flex items-center gap-2">
                <Users className="w-5 h-5 text-blue-600" />
                {editingCustomer ? 'Edit Customer' : 'Add Regular Customer'}
              </h3>
              <button
                onClick={handleCloseModal}
                className="text-slate-400 hover:text-slate-600 dark:hover:text-slate-200"
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            <form onSubmit={handleSubmit} className="space-y-4">
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">
                    Customer Name *
                  </label>
                  <input
                    type="text"
                    required
                    value={formData.name}
                    onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                    placeholder="e.g. Rahul Sharma"
                    className="w-full px-3 py-2 text-sm border border-slate-200 dark:border-slate-700 rounded-lg bg-white dark:bg-slate-900 text-slate-900 dark:text-slate-100"
                  />
                </div>

                <div>
                  <label className="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">
                    Email Address *
                  </label>
                  <input
                    type="email"
                    required
                    value={formData.email}
                    onChange={(e) => setFormData({ ...formData, email: e.target.value })}
                    placeholder="e.g. rahul@example.com"
                    className="w-full px-3 py-2 text-sm border border-slate-200 dark:border-slate-700 rounded-lg bg-white dark:bg-slate-900 text-slate-900 dark:text-slate-100"
                  />
                </div>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">
                    Mobile Number *
                  </label>
                  <input
                    type="tel"
                    required
                    value={formData.phone}
                    onChange={(e) => setFormData({ ...formData, phone: e.target.value })}
                    placeholder="e.g. 9876543210"
                    className="w-full px-3 py-2 text-sm border border-slate-200 dark:border-slate-700 rounded-lg bg-white dark:bg-slate-900 text-slate-900 dark:text-slate-100"
                  />
                </div>

                <div>
                  <label className="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">
                    Preferred Contact Method
                  </label>
                  <select
                    value={formData.preferredContactMethod}
                    onChange={(e) =>
                      setFormData({ ...formData, preferredContactMethod: parseInt(e.target.value) })
                    }
                    className="w-full px-3 py-2 text-sm border border-slate-200 dark:border-slate-700 rounded-lg bg-white dark:bg-slate-900 text-slate-900 dark:text-slate-100"
                  >
                    <option value={1}>Email</option>
                    <option value={2}>WhatsApp</option>
                    <option value={3}>SMS</option>
                    <option value={4}>Phone Call</option>
                  </select>
                </div>
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">
                  Delivery Address *
                </label>
                <textarea
                  rows={2}
                  required
                  value={formData.deliveryAddress}
                  onChange={(e) => setFormData({ ...formData, deliveryAddress: e.target.value })}
                  placeholder="Complete flat, building, street, area, city & pin code"
                  className="w-full px-3 py-2 text-sm border border-slate-200 dark:border-slate-700 rounded-lg bg-white dark:bg-slate-900 text-slate-900 dark:text-slate-100"
                />
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">
                  Billing Address (Optional)
                </label>
                <input
                  type="text"
                  value={formData.billingAddress}
                  onChange={(e) => setFormData({ ...formData, billingAddress: e.target.value })}
                  placeholder="Leave empty if same as delivery address"
                  className="w-full px-3 py-2 text-sm border border-slate-200 dark:border-slate-700 rounded-lg bg-white dark:bg-slate-900 text-slate-900 dark:text-slate-100"
                />
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">
                  Default Delivery Instructions
                </label>
                <input
                  type="text"
                  value={formData.defaultDeliveryInstructions}
                  onChange={(e) =>
                    setFormData({ ...formData, defaultDeliveryInstructions: e.target.value })
                  }
                  placeholder="e.g. Ring bell twice, leave with security guard if not home"
                  className="w-full px-3 py-2 text-sm border border-slate-200 dark:border-slate-700 rounded-lg bg-white dark:bg-slate-900 text-slate-900 dark:text-slate-100"
                />
              </div>

              <div className="flex items-center gap-3 p-3 bg-blue-50/70 dark:bg-blue-900/20 rounded-xl border border-blue-200 dark:border-blue-800">
                <input
                  type="checkbox"
                  id="recurringEnabled"
                  checked={formData.recurringOrderEnabled}
                  onChange={(e) =>
                    setFormData({ ...formData, recurringOrderEnabled: e.target.checked })
                  }
                  className="h-4 w-4 rounded border-slate-300 text-blue-600 focus:ring-blue-500"
                />
                <label htmlFor="recurringEnabled" className="text-sm font-medium text-blue-900 dark:text-blue-200">
                  Enable AI Recurring Order Subscription for this customer
                </label>
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">
                  Internal Notes
                </label>
                <textarea
                  rows={2}
                  value={formData.notes}
                  onChange={(e) => setFormData({ ...formData, notes: e.target.value })}
                  placeholder="Special preferences, payment terms, or customer history notes..."
                  className="w-full px-3 py-2 text-sm border border-slate-200 dark:border-slate-700 rounded-lg bg-white dark:bg-slate-900 text-slate-900 dark:text-slate-100"
                />
              </div>

              <div className="flex justify-end gap-3 pt-4 border-t border-slate-200 dark:border-slate-700">
                <button
                  type="button"
                  onClick={handleCloseModal}
                  className="px-4 py-2 border border-slate-200 dark:border-slate-700 text-slate-700 dark:text-slate-300 rounded-lg font-medium hover:bg-slate-50 dark:hover:bg-slate-900"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={createMutation.isPending || updateMutation.isPending}
                  className="flex items-center gap-2 px-5 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg font-medium transition disabled:opacity-50"
                >
                  {(createMutation.isPending || updateMutation.isPending) && (
                    <Loader2 className="w-4 h-4 animate-spin" />
                  )}
                  {editingCustomer ? 'Update Customer' : 'Save Customer'}
                </button>
              </div>
            </form>
          </div>
        </div>,
        document.body
      )}

      {/* Delete confirmation dialog */}
      <ConfirmDialog
        isOpen={!!deleteId}
        onClose={() => setDeleteId(null)}
        onConfirm={() => deleteId && deleteMutation.mutate(deleteId)}
        title="Remove Customer"
        message="Are you sure you want to remove this customer? If the customer has existing recurring or completed orders, they will be safely deactivated to preserve accounting records."
        confirmText="Remove"
        danger
        isLoading={deleteMutation.isPending}
      />
    </div>
  );
}
