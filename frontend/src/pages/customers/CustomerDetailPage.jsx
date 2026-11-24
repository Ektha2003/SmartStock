import React, { useState } from 'react';
import { createPortal } from 'react-dom';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  ArrowLeft,
  Users,
  Repeat,
  Package,
  Calendar,
  Mail,
  Phone,
  MapPin,
  Clock,
  Sparkles,
  Plus,
  Trash2,
  Edit2,
  CheckCircle2,
  AlertTriangle,
  Play,
  Pause,
  X,
  Loader2,
  Bot,
  ExternalLink,
} from 'lucide-react';
import customerService from '../../services/customerService';
import productService from '../../services/productService';
import recurringOrderService from '../../services/recurringOrderService';
import StatusBadge from '../../components/common/StatusBadge';
import ConfirmDialog from '../../components/common/ConfirmDialog';
import { toast } from 'sonner';

export default function CustomerDetailPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const [activeTab, setActiveTab] = useState('products'); // products, schedule, orders, ai-history

  // Modals for products
  const [isAddProductOpen, setIsAddProductOpen] = useState(false);
  const [editingProduct, setEditingProduct] = useState(null);
  const [deleteProductId, setDeleteProductId] = useState(null);

  // Add/Edit Product Form
  const [productFormData, setProductFormData] = useState({
    productId: '',
    defaultQuantity: 1,
    unit: 1, // Piece
    priceOverride: '',
    minQuantity: '',
    maxQuantity: '',
    productInstructions: '',
    isActive: true,
  });

  // Fetch Customer
  const { data: customer, isLoading: isCustomerLoading } = useQuery({
    queryKey: ['customer', id],
    queryFn: () => customerService.getCustomer(id),
  });

  // Fetch Recurring Products
  const { data: recurringProducts = [], isLoading: isProductsLoading } = useQuery({
    queryKey: ['customer-recurring-products', id],
    queryFn: () => customerService.getRecurringProducts(id),
  });

  // Fetch Recurring Setting
  const { data: recurringSetting, isLoading: isSettingLoading } = useQuery({
    queryKey: ['customer-recurring-setting', id],
    queryFn: () => customerService.getRecurringSetting(id),
  });

  // Fetch Available Store Catalog (for adding products)
  const { data: storeProductsData } = useQuery({
    queryKey: ['store-products-catalog'],
    queryFn: () => productService.getProducts({ pageSize: 100 }),
    enabled: isAddProductOpen,
  });

  const storeProducts = storeProductsData?.items || [];

  // Fetch Orders
  const { data: ordersData, isLoading: isOrdersLoading } = useQuery({
    queryKey: ['customer-orders', id],
    queryFn: () => customerService.getCustomerOrders(id),
    enabled: activeTab === 'orders',
  });

  const orders = ordersData?.items || [];

  // Fetch Email & AI Logs
  const { data: emailLogsData } = useQuery({
    queryKey: ['customer-email-logs', id],
    queryFn: () => recurringOrderService.getEmailLogs({ customerId: id }),
    enabled: activeTab === 'ai-history',
  });

  const emailLogs = emailLogsData?.items || [];

  const { data: aiLogsData } = useQuery({
    queryKey: ['customer-ai-logs', id],
    queryFn: () => recurringOrderService.getAILogs({ customerId: id }),
    enabled: activeTab === 'ai-history',
  });

  const aiLogs = aiLogsData?.items || [];

  // Mutations
  const addProductMutation = useMutation({
    mutationFn: (data) => customerService.addRecurringProduct(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['customer-recurring-products', id] });
      queryClient.invalidateQueries({ queryKey: ['customer', id] });
      toast.success('Product added to customer recurring list!');
      setIsAddProductOpen(false);
    },
    onError: (err) => toast.error(err.message || 'Failed to add product'),
  });

  const updateProductMutation = useMutation({
    mutationFn: ({ productId, data }) => customerService.updateRecurringProduct(id, productId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['customer-recurring-products', id] });
      toast.success('Recurring product configuration updated!');
      setEditingProduct(null);
    },
    onError: (err) => toast.error(err.message || 'Failed to update product'),
  });

  const removeProductMutation = useMutation({
    mutationFn: (productId) => customerService.removeRecurringProduct(id, productId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['customer-recurring-products', id] });
      queryClient.invalidateQueries({ queryKey: ['customer', id] });
      toast.success('Product removed from recurring list.');
      setDeleteProductId(null);
    },
    onError: (err) => toast.error(err.message || 'Failed to remove product'),
  });

  const updateSettingMutation = useMutation({
    mutationFn: (data) => customerService.updateRecurringSetting(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['customer-recurring-setting', id] });
      queryClient.invalidateQueries({ queryKey: ['customer', id] });
      toast.success('Recurring schedule saved successfully!');
    },
    onError: (err) => toast.error(err.message || 'Failed to update schedule'),
  });

  const toggleSubscriptionMutation = useMutation({
    mutationFn: (status) => customerService.toggleSubscriptionStatus(id, status),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['customer-recurring-setting', id] });
      queryClient.invalidateQueries({ queryKey: ['customer', id] });
      toast.success('Subscription status changed.');
    },
    onError: (err) => toast.error(err.message || 'Failed to change subscription'),
  });

  const generateDraftMutation = useMutation({
    mutationFn: () => recurringOrderService.generateDraftOrder(id),
    onSuccess: (res) => {
      queryClient.invalidateQueries({ queryKey: ['customer-orders', id] });
      queryClient.invalidateQueries({ queryKey: ['customer', id] });
      toast.success(`Draft order #${res.orderNumber} generated successfully!`);
      setActiveTab('orders');
    },
    onError: (err) => toast.error(err.message || 'Failed to generate draft order'),
  });

  // Schedule local edit form state
  const [scheduleState, setScheduleState] = useState(null);

  // Sync schedule form when setting loads
  React.useEffect(() => {
    if (recurringSetting) {
      setScheduleState({
        frequencyType: recurringSetting.frequencyType ?? 1,
        intervalDays: recurringSetting.intervalDays ?? 7,
        startDate: recurringSetting.startDate?.substring(0, 10) || new Date().toISOString().substring(0, 10),
        nextOrderDate: recurringSetting.nextOrderDate?.substring(0, 10) || '',
        preferredDeliveryTime: recurringSetting.preferredDeliveryTime || 'Morning 09:00 - 12:00',
        autoEmailEnabled: recurringSetting.autoEmailEnabled ?? true,
        requireCustomerApproval: recurringSetting.requireCustomerApproval ?? true,
        reminderBeforeDeliveryDays: recurringSetting.reminderBeforeDeliveryDays ?? 1,
        timeZone: recurringSetting.timeZone || 'Asia/Kolkata',
        subscriptionStatus: recurringSetting.subscriptionStatus ?? 1,
      });
    }
  }, [recurringSetting]);

  if (isCustomerLoading) {
    return (
      <div className="flex items-center justify-center p-12">
        <Loader2 className="w-8 h-8 animate-spin text-blue-600" />
      </div>
    );
  }

  if (!customer) {
    return (
      <div className="text-center p-12">
        <p className="text-slate-600 dark:text-slate-400">Customer not found.</p>
        <button
          onClick={() => navigate('/customers')}
          className="mt-4 px-4 py-2 bg-blue-600 text-white rounded-lg"
        >
          Back to Customers
        </button>
      </div>
    );
  }

  const handleOpenAddProduct = () => {
    setProductFormData({
      productId: '',
      defaultQuantity: 1,
      unit: 1,
      priceOverride: '',
      minQuantity: '',
      maxQuantity: '',
      productInstructions: '',
      isActive: true,
    });
    setIsAddProductOpen(true);
  };

  const handleOpenEditProduct = (p) => {
    setEditingProduct(p);
    setProductFormData({
      productId: p.productId,
      defaultQuantity: p.defaultQuantity,
      unit: p.unit,
      priceOverride: p.priceOverride || '',
      minQuantity: p.minQuantity || '',
      maxQuantity: p.maxQuantity || '',
      productInstructions: p.productInstructions || '',
      isActive: p.isActive,
    });
  };

  const handleSaveProduct = (e) => {
    e.preventDefault();
    if (!productFormData.productId) {
      toast.error('Please select an inventory product.');
      return;
    }

    const payload = {
      productId: productFormData.productId,
      defaultQuantity: parseFloat(productFormData.defaultQuantity) || 1,
      unit: parseInt(productFormData.unit),
      priceOverride: productFormData.priceOverride ? parseFloat(productFormData.priceOverride) : null,
      minQuantity: productFormData.minQuantity ? parseFloat(productFormData.minQuantity) : null,
      maxQuantity: productFormData.maxQuantity ? parseFloat(productFormData.maxQuantity) : null,
      productInstructions: productFormData.productInstructions?.trim(),
      isActive: productFormData.isActive,
    };

    if (editingProduct) {
      updateProductMutation.mutate({
        productId: editingProduct.id,
        data: payload,
      });
    } else {
      addProductMutation.mutate(payload);
    }
  };

  const handleSaveSchedule = (e) => {
    e.preventDefault();
    if (!scheduleState) return;

    updateSettingMutation.mutate({
      frequencyType: parseInt(scheduleState.frequencyType),
      intervalDays: parseInt(scheduleState.intervalDays) || 7,
      startDate: new Date(scheduleState.startDate).toISOString(),
      nextOrderDate: scheduleState.nextOrderDate ? new Date(scheduleState.nextOrderDate).toISOString() : new Date().toISOString(),
      preferredDeliveryTime: scheduleState.preferredDeliveryTime,
      autoEmailEnabled: scheduleState.autoEmailEnabled,
      requireCustomerApproval: scheduleState.requireCustomerApproval,
      reminderBeforeDeliveryDays: parseInt(scheduleState.reminderBeforeDeliveryDays) || 1,
      timeZone: scheduleState.timeZone,
      subscriptionStatus: parseInt(scheduleState.subscriptionStatus),
    });
  };

  return (
    <div className="space-y-6">
      {/* Top Navigation & Profile Header */}
      <div>
        <button
          onClick={() => navigate('/customers')}
          className="inline-flex items-center gap-1.5 text-sm text-slate-500 hover:text-slate-800 dark:hover:text-slate-200 transition mb-3"
        >
          <ArrowLeft className="w-4 h-4" />
          Back to Customers
        </button>

        <div className="bg-white dark:bg-slate-800 rounded-2xl border border-slate-200 dark:border-slate-700 p-6 shadow-sm">
          <div className="flex flex-col lg:flex-row items-start lg:items-center justify-between gap-4">
            <div className="flex items-center gap-4">
              <div className="h-14 w-14 rounded-2xl bg-gradient-to-tr from-blue-600 to-indigo-600 text-white font-bold text-xl flex items-center justify-center shadow-md">
                {customer.name.substring(0, 2).toUpperCase()}
              </div>
              <div>
                <div className="flex items-center gap-3">
                  <h1 className="text-2xl font-bold text-slate-900 dark:text-slate-100">
                    {customer.name}
                  </h1>
                  <StatusBadge status={customer.isActive ? 'Active' : 'Inactive'} />
                  {customer.recurringOrderEnabled && (
                    <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-semibold bg-emerald-100 text-emerald-800 dark:bg-emerald-900/40 dark:text-emerald-300">
                      <Repeat className="w-3 h-3" />
                      Recurring Customer
                    </span>
                  )}
                </div>

                <div className="flex flex-wrap items-center gap-4 text-xs text-slate-500 mt-2">
                  <span className="flex items-center gap-1">
                    <Mail className="w-3.5 h-3.5 text-slate-400" />
                    {customer.email}
                  </span>
                  <span className="flex items-center gap-1">
                    <Phone className="w-3.5 h-3.5 text-slate-400" />
                    {customer.phone}
                  </span>
                  <span className="flex items-center gap-1">
                    <MapPin className="w-3.5 h-3.5 text-slate-400" />
                    {customer.deliveryAddress}
                  </span>
                </div>
              </div>
            </div>

            {/* Quick action button: Generate Draft Order Now */}
            <div className="flex items-center gap-3">
              <button
                onClick={() => generateDraftMutation.mutate()}
                disabled={generateDraftMutation.isPending || !customer.recurringOrderEnabled}
                className="flex items-center gap-2 px-4 py-2 bg-emerald-600 hover:bg-emerald-700 text-white rounded-lg font-medium shadow-sm transition disabled:opacity-50"
              >
                {generateDraftMutation.isPending ? (
                  <Loader2 className="w-4 h-4 animate-spin" />
                ) : (
                  <Sparkles className="w-4 h-4" />
                )}
                Generate Order Draft Now
              </button>
            </div>
          </div>
        </div>
      </div>

      {/* Navigation Tabs */}
      <div className="border-b border-slate-200 dark:border-slate-700 flex gap-6">
        {[
          { key: 'products', label: 'Recurring Products', icon: Package, count: recurringProducts.length },
          { key: 'schedule', label: 'Recurring Schedule', icon: Calendar },
          { key: 'orders', label: 'Order History', icon: Repeat, count: customer.totalOrdersCount },
          { key: 'ai-history', label: 'AI & Email Activity', icon: Bot, count: aiLogs.length + emailLogs.length },
        ].map((tab) => {
          const Icon = tab.icon;
          const isActive = activeTab === tab.key;
          return (
            <button
              key={tab.key}
              onClick={() => setActiveTab(tab.key)}
              className={`flex items-center gap-2 pb-3.5 text-sm font-semibold transition border-b-2 -mb-px ${
                isActive
                  ? 'border-blue-600 text-blue-600 dark:text-blue-400'
                  : 'border-transparent text-slate-500 hover:text-slate-800 dark:hover:text-slate-200'
              }`}
            >
              <Icon className="w-4 h-4" />
              {tab.label}
              {tab.count !== undefined && (
                <span
                  className={`px-2 py-0.5 rounded-full text-xs ${
                    isActive
                      ? 'bg-blue-100 text-blue-700 dark:bg-blue-900/50 dark:text-blue-300'
                      : 'bg-slate-100 text-slate-600 dark:bg-slate-800 dark:text-slate-400'
                  }`}
                >
                  {tab.count}
                </span>
              )}
            </button>
          );
        })}
      </div>

      {/* TAB 1: RECURRING PRODUCTS */}
      {activeTab === 'products' && (
        <div className="space-y-4">
          <div className="flex items-center justify-between">
            <div>
              <h2 className="text-base font-bold text-slate-900 dark:text-slate-100">
                Predefined Recurring Basket
              </h2>
              <p className="text-xs text-slate-500">
                Products automatically included whenever a new recurring delivery cycle generates.
              </p>
            </div>
            <button
              onClick={handleOpenAddProduct}
              className="flex items-center gap-1.5 px-3.5 py-1.5 bg-blue-600 hover:bg-blue-700 text-white rounded-lg text-sm font-medium transition"
            >
              <Plus className="w-4 h-4" />
              Add Product
            </button>
          </div>

          <div className="bg-white dark:bg-slate-800 rounded-xl border border-slate-200 dark:border-slate-700 shadow-sm overflow-hidden">
            <table className="w-full text-left text-sm">
              <thead className="bg-slate-50 dark:bg-slate-900/50 text-slate-600 dark:text-slate-400 text-xs font-semibold uppercase tracking-wider border-b border-slate-200 dark:border-slate-700">
                <tr>
                  <th className="px-4 py-3">Product Name</th>
                  <th className="px-4 py-3">Default Qty</th>
                  <th className="px-4 py-3">Unit Price</th>
                  <th className="px-4 py-3">Cycle Subtotal</th>
                  <th className="px-4 py-3">Current Stock</th>
                  <th className="px-4 py-3">Instructions</th>
                  <th className="px-4 py-3">Status</th>
                  <th className="px-4 py-3 text-right">Actions</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-200 dark:divide-slate-700">
                {recurringProducts.length === 0 ? (
                  <tr>
                    <td colSpan={8} className="px-4 py-8 text-center text-slate-500">
                      No recurring products configured yet. Click "Add Product" to select items from inventory.
                    </td>
                  </tr>
                ) : (
                  recurringProducts.map((p) => {
                    const cycleSubtotal = p.defaultQuantity * p.effectivePrice;
                    return (
                      <tr key={p.id} className="hover:bg-slate-50/50 dark:hover:bg-slate-900/30">
                        <td className="px-4 py-3">
                          <div className="font-semibold text-slate-900 dark:text-slate-100">
                            {p.productName}
                          </div>
                          <div className="text-xs text-slate-500">SKU: {p.productSku}</div>
                        </td>
                        <td className="px-4 py-3 font-semibold text-slate-800 dark:text-slate-200">
                          {p.defaultQuantity} {p.unitName}
                        </td>
                        <td className="px-4 py-3">
                          <span className="font-medium text-slate-900 dark:text-slate-100">
                            ₹{p.effectivePrice.toFixed(2)}
                          </span>
                          {p.priceOverride && (
                            <span className="ml-1 text-[11px] text-blue-600 bg-blue-50 dark:bg-blue-900/40 px-1.5 py-0.5 rounded">
                              Custom
                            </span>
                          )}
                        </td>
                        <td className="px-4 py-3 font-semibold text-emerald-600 dark:text-emerald-400">
                          ₹{cycleSubtotal.toFixed(2)}
                        </td>
                        <td className="px-4 py-3">
                          {p.inStock ? (
                            <span className="inline-flex items-center gap-1 text-xs text-emerald-600 dark:text-emerald-400 font-medium">
                              <CheckCircle2 className="w-3.5 h-3.5" />
                              {p.currentStock} in stock
                            </span>
                          ) : (
                            <span className="inline-flex items-center gap-1 text-xs text-amber-600 dark:text-amber-400 font-medium">
                              <AlertTriangle className="w-3.5 h-3.5" />
                              Low: {p.currentStock}
                            </span>
                          )}
                        </td>
                        <td className="px-4 py-3 text-xs text-slate-500 max-w-xs truncate">
                          {p.productInstructions || '-'}
                        </td>
                        <td className="px-4 py-3">
                          <StatusBadge status={p.isActive ? 'Active' : 'Inactive'} />
                        </td>
                        <td className="px-4 py-3 text-right">
                          <div className="flex items-center justify-end gap-2">
                            <button
                              onClick={() => handleOpenEditProduct(p)}
                              className="p-1 text-slate-500 hover:text-blue-600"
                              title="Edit item"
                            >
                              <Edit2 className="w-4 h-4" />
                            </button>
                            <button
                              onClick={() => setDeleteProductId(p.id)}
                              className="p-1 text-slate-500 hover:text-red-600"
                              title="Remove item"
                            >
                              <Trash2 className="w-4 h-4" />
                            </button>
                          </div>
                        </td>
                      </tr>
                    );
                  })
                )}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* TAB 2: RECURRING SCHEDULE */}
      {activeTab === 'schedule' && scheduleState && (
        <form onSubmit={handleSaveSchedule} className="space-y-6">
          <div className="bg-white dark:bg-slate-800 rounded-xl border border-slate-200 dark:border-slate-700 p-6 shadow-sm space-y-5">
            <h3 className="text-base font-bold text-slate-900 dark:text-slate-100 flex items-center gap-2">
              <Calendar className="w-5 h-5 text-blue-600" />
              Subscription Frequency & Scheduling Rules
            </h3>

            <div className="grid grid-cols-1 md:grid-cols-3 gap-5">
              <div>
                <label className="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">
                  Recurring Frequency *
                </label>
                <select
                  value={scheduleState.frequencyType}
                  onChange={(e) =>
                    setScheduleState({ ...scheduleState, frequencyType: parseInt(e.target.value) })
                  }
                  className="w-full px-3 py-2 text-sm border border-slate-200 dark:border-slate-700 rounded-lg bg-white dark:bg-slate-900 text-slate-900 dark:text-slate-100"
                >
                  <option value={1}>Every 7 Days (Weekly)</option>
                  <option value={2}>Every 15 Days (Fortnightly)</option>
                  <option value={3}>Every 30 Days (Monthly)</option>
                  <option value={4}>Every Month (Calendar Month)</option>
                  <option value={5}>Every 2 Months (Bi-monthly)</option>
                  <option value={6}>Custom Interval Days</option>
                </select>
              </div>

              {scheduleState.frequencyType === 6 && (
                <div>
                  <label className="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">
                    Custom Interval (Days) *
                  </label>
                  <input
                    type="number"
                    min={1}
                    value={scheduleState.intervalDays}
                    onChange={(e) =>
                      setScheduleState({ ...scheduleState, intervalDays: e.target.value })
                    }
                    className="w-full px-3 py-2 text-sm border border-slate-200 dark:border-slate-700 rounded-lg bg-white dark:bg-slate-900 text-slate-900 dark:text-slate-100"
                  />
                </div>
              )}

              <div>
                <label className="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">
                  Start Date *
                </label>
                <input
                  type="date"
                  value={scheduleState.startDate}
                  onChange={(e) =>
                    setScheduleState({ ...scheduleState, startDate: e.target.value })
                  }
                  className="w-full px-3 py-2 text-sm border border-slate-200 dark:border-slate-700 rounded-lg bg-white dark:bg-slate-900 text-slate-900 dark:text-slate-100"
                />
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">
                  Next Order Scheduled Date
                </label>
                <input
                  type="date"
                  value={scheduleState.nextOrderDate}
                  onChange={(e) =>
                    setScheduleState({ ...scheduleState, nextOrderDate: e.target.value })
                  }
                  className="w-full px-3 py-2 text-sm border border-slate-200 dark:border-slate-700 rounded-lg bg-white dark:bg-slate-900 text-slate-900 dark:text-slate-100"
                />
              </div>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-5 pt-3 border-t border-slate-100 dark:border-slate-700">
              <div>
                <label className="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">
                  Preferred Delivery Slot / Time
                </label>
                <input
                  type="text"
                  value={scheduleState.preferredDeliveryTime}
                  onChange={(e) =>
                    setScheduleState({ ...scheduleState, preferredDeliveryTime: e.target.value })
                  }
                  placeholder="e.g. Morning 09:00 - 12:00 or Evening 17:00 - 20:00"
                  className="w-full px-3 py-2 text-sm border border-slate-200 dark:border-slate-700 rounded-lg bg-white dark:bg-slate-900 text-slate-900 dark:text-slate-100"
                />
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">
                  Reminder Days Before Delivery
                </label>
                <input
                  type="number"
                  min={0}
                  max={7}
                  value={scheduleState.reminderBeforeDeliveryDays}
                  onChange={(e) =>
                    setScheduleState({
                      ...scheduleState,
                      reminderBeforeDeliveryDays: e.target.value,
                    })
                  }
                  className="w-full px-3 py-2 text-sm border border-slate-200 dark:border-slate-700 rounded-lg bg-white dark:bg-slate-900 text-slate-900 dark:text-slate-100"
                />
              </div>
            </div>

            <div className="space-y-3 pt-3 border-t border-slate-100 dark:border-slate-700">
              <label className="flex items-center gap-3">
                <input
                  type="checkbox"
                  checked={scheduleState.autoEmailEnabled}
                  onChange={(e) =>
                    setScheduleState({ ...scheduleState, autoEmailEnabled: e.target.checked })
                  }
                  className="h-4 w-4 rounded border-slate-300 text-blue-600 focus:ring-blue-500"
                />
                <span className="text-sm text-slate-800 dark:text-slate-200">
                  Automatically send AI personalized order confirmation email when cycle is due
                </span>
              </label>

              <label className="flex items-center gap-3">
                <input
                  type="checkbox"
                  checked={scheduleState.requireCustomerApproval}
                  onChange={(e) =>
                    setScheduleState({
                      ...scheduleState,
                      requireCustomerApproval: e.target.checked,
                    })
                  }
                  className="h-4 w-4 rounded border-slate-300 text-blue-600 focus:ring-blue-500"
                />
                <span className="text-sm text-slate-800 dark:text-slate-200">
                  Require explicit customer approval (or natural language reply) before order dispatch
                </span>
              </label>
            </div>

            <div className="pt-4 flex items-center justify-between">
              <div className="flex items-center gap-2">
                <span className="text-xs font-semibold text-slate-600 dark:text-slate-400">
                  Subscription Status:
                </span>
                <span
                  className={`px-3 py-1 rounded-full text-xs font-bold ${
                    scheduleState.subscriptionStatus === 1
                      ? 'bg-emerald-100 text-emerald-800'
                      : scheduleState.subscriptionStatus === 2
                      ? 'bg-amber-100 text-amber-800'
                      : 'bg-rose-100 text-rose-800'
                  }`}
                >
                  {scheduleState.subscriptionStatus === 1
                    ? 'ACTIVE'
                    : scheduleState.subscriptionStatus === 2
                    ? 'PAUSED'
                    : 'CANCELLED'}
                </span>
              </div>

              <div className="flex gap-2">
                {scheduleState.subscriptionStatus === 1 ? (
                  <button
                    type="button"
                    onClick={() => toggleSubscriptionMutation.mutate(2)} // Pause
                    className="flex items-center gap-1.5 px-3 py-1.5 border border-amber-300 text-amber-700 dark:text-amber-400 rounded-lg text-xs font-semibold hover:bg-amber-50"
                  >
                    <Pause className="w-3.5 h-3.5" />
                    Pause Subscription
                  </button>
                ) : (
                  <button
                    type="button"
                    onClick={() => toggleSubscriptionMutation.mutate(1)} // Resume
                    className="flex items-center gap-1.5 px-3 py-1.5 border border-emerald-300 text-emerald-700 dark:text-emerald-400 rounded-lg text-xs font-semibold hover:bg-emerald-50"
                  >
                    <Play className="w-3.5 h-3.5" />
                    Activate Subscription
                  </button>
                )}

                <button
                  type="submit"
                  disabled={updateSettingMutation.isPending}
                  className="flex items-center gap-2 px-5 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg text-sm font-semibold transition disabled:opacity-50"
                >
                  {updateSettingMutation.isPending && (
                    <Loader2 className="w-4 h-4 animate-spin" />
                  )}
                  Save Schedule Rules
                </button>
              </div>
            </div>
          </div>
        </form>
      )}

      {/* TAB 3: ORDER HISTORY */}
      {activeTab === 'orders' && (
        <div className="space-y-4">
          <div className="bg-white dark:bg-slate-800 rounded-xl border border-slate-200 dark:border-slate-700 shadow-sm overflow-hidden">
            <table className="w-full text-left text-sm">
              <thead className="bg-slate-50 dark:bg-slate-900/50 text-slate-600 dark:text-slate-400 text-xs font-semibold uppercase tracking-wider border-b border-slate-200 dark:border-slate-700">
                <tr>
                  <th className="px-4 py-3">Order Number</th>
                  <th className="px-4 py-3">Scheduled Date</th>
                  <th className="px-4 py-3">Expected Delivery</th>
                  <th className="px-4 py-3">Items Count</th>
                  <th className="px-4 py-3">Total Amount</th>
                  <th className="px-4 py-3">Status</th>
                  <th className="px-4 py-3 text-right">Actions</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-200 dark:divide-slate-700">
                {orders.length === 0 ? (
                  <tr>
                    <td colSpan={7} className="px-4 py-8 text-center text-slate-500">
                      No order history found for this customer.
                    </td>
                  </tr>
                ) : (
                  orders.map((o) => (
                    <tr key={o.id} className="hover:bg-slate-50/50 dark:hover:bg-slate-900/30">
                      <td className="px-4 py-3 font-semibold text-blue-600">
                        {o.orderNumber}
                      </td>
                      <td className="px-4 py-3 text-slate-600 dark:text-slate-300">
                        {new Date(o.scheduledOrderDate).toLocaleDateString()}
                      </td>
                      <td className="px-4 py-3 text-slate-600 dark:text-slate-300">
                        {new Date(o.expectedDeliveryDate).toLocaleDateString()}
                      </td>
                      <td className="px-4 py-3 font-medium">
                        {o.items?.length || 0} items
                      </td>
                      <td className="px-4 py-3 font-bold text-slate-900 dark:text-slate-100">
                        ₹{o.totalAmount.toFixed(2)}
                      </td>
                      <td className="px-4 py-3">
                        <StatusBadge status={o.statusName} />
                      </td>
                      <td className="px-4 py-3 text-right">
                        <button
                          onClick={() => navigate('/recurring-orders')}
                          className="text-xs text-blue-600 hover:underline font-semibold"
                        >
                          View in Orders
                        </button>
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* TAB 4: AI & EMAIL ACTIVITY */}
      {activeTab === 'ai-history' && (
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          {/* Email Communications */}
          <div className="bg-white dark:bg-slate-800 rounded-xl border border-slate-200 dark:border-slate-700 p-5 shadow-sm space-y-4">
            <h3 className="text-sm font-bold text-slate-900 dark:text-slate-100 flex items-center gap-2">
              <Mail className="w-4 h-4 text-blue-600" />
              Email Communication History
            </h3>
            {emailLogs.length === 0 ? (
              <p className="text-xs text-slate-500 text-center py-6">No emails sent or received yet.</p>
            ) : (
              <div className="space-y-3">
                {emailLogs.map((log) => (
                  <div
                    key={log.id}
                    className="p-3 rounded-lg border border-slate-100 dark:border-slate-700/60 bg-slate-50/50 dark:bg-slate-900/30 text-xs space-y-1.5"
                  >
                    <div className="flex items-center justify-between">
                      <span
                        className={`font-semibold px-2 py-0.5 rounded text-[10px] ${
                          log.directionName === 'Outbound'
                            ? 'bg-blue-100 text-blue-700 dark:bg-blue-900/50 dark:text-blue-300'
                            : 'bg-emerald-100 text-emerald-700 dark:bg-emerald-900/50 dark:text-emerald-300'
                        }`}
                      >
                        {log.directionName}
                      </span>
                      <span className="text-slate-400 text-[11px]">
                        {new Date(log.createdAt).toLocaleString()}
                      </span>
                    </div>
                    <div className="font-medium text-slate-800 dark:text-slate-200 truncate">
                      {log.subject}
                    </div>
                    <div className="text-slate-500 text-[11px] flex justify-between items-center">
                      <span>Status: {log.statusName}</span>
                      {log.orderNumber && <span>Order: {log.orderNumber}</span>}
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>

          {/* AI NLP Classifier Logs */}
          <div className="bg-white dark:bg-slate-800 rounded-xl border border-slate-200 dark:border-slate-700 p-5 shadow-sm space-y-4">
            <h3 className="text-sm font-bold text-slate-900 dark:text-slate-100 flex items-center gap-2">
              <Sparkles className="w-4 h-4 text-purple-600" />
              AI Natural Language Reply Logs
            </h3>
            {aiLogs.length === 0 ? (
              <p className="text-xs text-slate-500 text-center py-6">No customer replies parsed yet.</p>
            ) : (
              <div className="space-y-3">
                {aiLogs.map((ai) => (
                  <div
                    key={ai.id}
                    className="p-3 rounded-lg border border-purple-100 dark:border-purple-900/40 bg-purple-50/20 dark:bg-purple-950/20 text-xs space-y-2"
                  >
                    <div className="flex items-center justify-between">
                      <span className="font-bold text-purple-700 dark:text-purple-300 text-[11px]">
                        Intent: {ai.parsedIntent}
                      </span>
                      <span className="px-2 py-0.5 rounded-full text-[10px] bg-purple-100 text-purple-800 dark:bg-purple-900/60 dark:text-purple-300 font-semibold">
                        {(ai.confidenceScore * 100).toFixed(0)}% Confidence
                      </span>
                    </div>

                    <div className="p-2 bg-white dark:bg-slate-900 rounded border border-slate-200 dark:border-slate-700 text-slate-700 dark:text-slate-300 italic">
                      "{ai.rawEmailText}"
                    </div>

                    <div className="text-[11px] text-slate-600 dark:text-slate-400">
                      <strong>Result:</strong> {ai.executionSummary || 'Logged for review'}
                    </div>

                    {ai.requiresManualReview && (
                      <div className="text-[11px] text-rose-600 dark:text-rose-400 font-medium">
                        ⚠️ Flagged for manual review: {ai.manualReviewReason}
                      </div>
                    )}
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>
      )}

      {/* Add / Edit Recurring Product Modal */}
      {(isAddProductOpen || editingProduct) && createPortal(
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/50 backdrop-blur-sm">
          <div className="bg-white dark:bg-slate-800 rounded-2xl max-w-lg w-full p-6 shadow-xl border border-slate-200 dark:border-slate-700">
            <div className="flex items-center justify-between pb-3 border-b border-slate-200 dark:border-slate-700 mb-4">
              <h3 className="text-base font-bold text-slate-900 dark:text-slate-100 flex items-center gap-2">
                <Package className="w-5 h-5 text-blue-600" />
                {editingProduct ? 'Edit Recurring Product' : 'Add Product to Recurring Basket'}
              </h3>
              <button
                onClick={() => {
                  setIsAddProductOpen(false);
                  setEditingProduct(null);
                }}
                className="text-slate-400 hover:text-slate-600"
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            <form onSubmit={handleSaveProduct} className="space-y-4">
              <div>
                <label className="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">
                  Select Product from Inventory *
                </label>
                {editingProduct ? (
                  <div className="px-3 py-2 text-sm bg-slate-100 dark:bg-slate-900 rounded-lg text-slate-900 dark:text-slate-100 font-semibold">
                    {editingProduct.productName} ({editingProduct.productSku})
                  </div>
                ) : (
                  <select
                    required
                    value={productFormData.productId}
                    onChange={(e) => {
                      const p = storeProducts.find((x) => x.id === e.target.value);
                      setProductFormData({
                        ...productFormData,
                        productId: e.target.value,
                        unit: p?.unit || 1,
                      });
                    }}
                    className="w-full px-3 py-2 text-sm border border-slate-200 dark:border-slate-700 rounded-lg bg-white dark:bg-slate-900 text-slate-900 dark:text-slate-100"
                  >
                    <option value="">-- Choose from catalog --</option>
                    {storeProducts.map((p) => (
                      <option key={p.id} value={p.id}>
                        {p.name} - ₹{p.sellingPrice} (Stock: {p.currentStock} {p.unitName})
                      </option>
                    ))}
                  </select>
                )}
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">
                    Default Quantity *
                  </label>
                  <input
                    type="number"
                    step="0.01"
                    min="0.1"
                    required
                    value={productFormData.defaultQuantity}
                    onChange={(e) =>
                      setProductFormData({ ...productFormData, defaultQuantity: e.target.value })
                    }
                    className="w-full px-3 py-2 text-sm border border-slate-200 dark:border-slate-700 rounded-lg bg-white dark:bg-slate-900 text-slate-900 dark:text-slate-100"
                  />
                </div>

                <div>
                  <label className="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">
                    Unit of Measurement *
                  </label>
                  <select
                    value={productFormData.unit}
                    onChange={(e) =>
                      setProductFormData({ ...productFormData, unit: parseInt(e.target.value) })
                    }
                    className="w-full px-3 py-2 text-sm border border-slate-200 dark:border-slate-700 rounded-lg bg-white dark:bg-slate-900 text-slate-900 dark:text-slate-100"
                  >
                    <option value={1}>Piece</option>
                    <option value={2}>KG</option>
                    <option value={3}>Gram</option>
                    <option value={4}>Litre</option>
                    <option value={5}>Box</option>
                    <option value={6}>Packet</option>
                  </select>
                </div>
              </div>

              <div className="grid grid-cols-3 gap-3">
                <div>
                  <label className="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">
                    Price Override (₹)
                  </label>
                  <input
                    type="number"
                    step="0.01"
                    placeholder="Auto catalog"
                    value={productFormData.priceOverride}
                    onChange={(e) =>
                      setProductFormData({ ...productFormData, priceOverride: e.target.value })
                    }
                    className="w-full px-3 py-2 text-sm border border-slate-200 dark:border-slate-700 rounded-lg bg-white dark:bg-slate-900 text-slate-900 dark:text-slate-100"
                  />
                </div>

                <div>
                  <label className="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">
                    Min Qty
                  </label>
                  <input
                    type="number"
                    step="0.1"
                    placeholder="Optional"
                    value={productFormData.minQuantity}
                    onChange={(e) =>
                      setProductFormData({ ...productFormData, minQuantity: e.target.value })
                    }
                    className="w-full px-3 py-2 text-sm border border-slate-200 dark:border-slate-700 rounded-lg bg-white dark:bg-slate-900 text-slate-900 dark:text-slate-100"
                  />
                </div>

                <div>
                  <label className="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">
                    Max Qty
                  </label>
                  <input
                    type="number"
                    step="0.1"
                    placeholder="Optional"
                    value={productFormData.maxQuantity}
                    onChange={(e) =>
                      setProductFormData({ ...productFormData, maxQuantity: e.target.value })
                    }
                    className="w-full px-3 py-2 text-sm border border-slate-200 dark:border-slate-700 rounded-lg bg-white dark:bg-slate-900 text-slate-900 dark:text-slate-100"
                  />
                </div>
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">
                  Product Specific Instructions
                </label>
                <input
                  type="text"
                  placeholder="e.g. Preferred brand, ripe fruits, fine grain"
                  value={productFormData.productInstructions}
                  onChange={(e) =>
                    setProductFormData({ ...productFormData, productInstructions: e.target.value })
                  }
                  className="w-full px-3 py-2 text-sm border border-slate-200 dark:border-slate-700 rounded-lg bg-white dark:bg-slate-900 text-slate-900 dark:text-slate-100"
                />
              </div>

              <div className="flex items-center gap-2 pt-2">
                <input
                  type="checkbox"
                  id="prodActive"
                  checked={productFormData.isActive}
                  onChange={(e) =>
                    setProductFormData({ ...productFormData, isActive: e.target.checked })
                  }
                  className="h-4 w-4 rounded border-slate-300 text-blue-600 focus:ring-blue-500"
                />
                <label htmlFor="prodActive" className="text-xs font-semibold text-slate-700 dark:text-slate-300">
                  Active in Recurring Basket
                </label>
              </div>

              <div className="flex justify-end gap-3 pt-3 border-t border-slate-200 dark:border-slate-700">
                <button
                  type="button"
                  onClick={() => {
                    setIsAddProductOpen(false);
                    setEditingProduct(null);
                  }}
                  className="px-4 py-2 border border-slate-200 dark:border-slate-700 rounded-lg text-sm font-medium text-slate-700 dark:text-slate-300"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={addProductMutation.isPending || updateProductMutation.isPending}
                  className="px-5 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg text-sm font-semibold transition disabled:opacity-50"
                >
                  Save Product
                </button>
              </div>
            </form>
          </div>
        </div>,
        document.body
      )}

      {/* Remove Product Confirmation */}
      <ConfirmDialog
        isOpen={!!deleteProductId}
        onClose={() => setDeleteProductId(null)}
        onConfirm={() => deleteProductId && removeProductMutation.mutate(deleteProductId)}
        title="Remove Product"
        message="Are you sure you want to remove this product from the customer's recurring basket?"
        confirmText="Remove"
        danger
        isLoading={removeProductMutation.isPending}
      />
    </div>
  );
}
