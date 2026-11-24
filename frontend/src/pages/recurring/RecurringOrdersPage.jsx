import React, { useState, useMemo } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import {
  Repeat,
  Users,
  CheckCircle2,
  Clock,
  AlertTriangle,
  Play,
  RotateCcw,
  Sparkles,
  Bot,
  Filter,
  Eye,
  Check,
  X,
  Send,
  Loader2,
  ExternalLink,
  MessageSquare,
  AlertCircle,
  TrendingUp,
} from 'lucide-react';
import recurringOrderService from '../../services/recurringOrderService';
import PageHeader from '../../components/common/PageHeader';
import DataTable from '../../components/common/DataTable';
import SearchInput from '../../components/common/SearchInput';
import StatusBadge from '../../components/common/StatusBadge';
import Pagination from '../../components/common/Pagination';
import ConfirmDialog from '../../components/common/ConfirmDialog';
import { toast } from 'sonner';

export default function RecurringOrdersPage() {
  const queryClient = useQueryClient();
  const navigate = useNavigate();

  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState('');
  const [manualReviewFilter, setManualReviewFilter] = useState(false);
  const [frequencyFilter, setFrequencyFilter] = useState('');
  const [currentPage, setCurrentPage] = useState(1);
  const pageSize = 10;

  // Selected order for details modal
  const [viewingOrder, setViewingOrder] = useState(null);
  // Selected order for manual review resolution modal
  const [reviewOrder, setReviewOrder] = useState(null);
  const [reviewNotes, setReviewNotes] = useState('');

  // Simulator Modal
  const [isSimulatorOpen, setIsSimulatorOpen] = useState(false);
  const [simEmail, setSimEmail] = useState('');
  const [simBody, setSimBody] = useState('');
  const [simResult, setSimResult] = useState(null);

  // Fetch Dashboard Metrics
  const { data: metrics } = useQuery({
    queryKey: ['recurring-metrics'],
    queryFn: () => recurringOrderService.getDashboardMetrics(),
    refetchInterval: 15000,
  });

  // Fetch Orders Table
  const { data: ordersData, isLoading } = useQuery({
    queryKey: ['recurring-orders', search, statusFilter, manualReviewFilter, frequencyFilter, currentPage],
    queryFn: () =>
      recurringOrderService.getRecurringOrders({
        search: search || undefined,
        status: statusFilter ? parseInt(statusFilter) : undefined,
        requiresManualReview: manualReviewFilter ? true : undefined,
        frequency: frequencyFilter ? parseInt(frequencyFilter) : undefined,
        pageNumber: currentPage,
        pageSize,
      }),
  });

  const orders = useMemo(() => {
    if (Array.isArray(ordersData)) return ordersData;
    if (ordersData?.items && Array.isArray(ordersData.items)) return ordersData.items;
    return [];
  }, [ordersData]);

  const totalPages = ordersData?.totalPages || 1;
  const totalCount = ordersData?.totalCount || orders.length;

  // Mutations
  const triggerCycleMutation = useMutation({
    mutationFn: () => recurringOrderService.triggerSchedulerCycle(),
    onSuccess: (res) => {
      queryClient.invalidateQueries({ queryKey: ['recurring-orders'] });
      queryClient.invalidateQueries({ queryKey: ['recurring-metrics'] });
      toast.success(res.message || 'Automation cycle completed successfully!');
    },
    onError: (err) => toast.error(err.message || 'Automation cycle failed'),
  });

  const finalizeToSaleMutation = useMutation({
    mutationFn: (id) => recurringOrderService.finalizeOrderToSale(id),
    onSuccess: (res) => {
      queryClient.invalidateQueries({ queryKey: ['recurring-orders'] });
      queryClient.invalidateQueries({ queryKey: ['recurring-metrics'] });
      queryClient.invalidateQueries({ queryKey: ['sales'] });
      toast.success(`Order finalized! Sales invoice ${res.invoiceNumber} created & inventory deducted.`);
      setViewingOrder(null);
    },
    onError: (err) => toast.error(err.message || 'Failed to finalize order to sale'),
  });

  const resolveReviewMutation = useMutation({
    mutationFn: ({ id, approve, notes }) =>
      recurringOrderService.resolveManualReview(id, { approve, adminNotes: notes }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['recurring-orders'] });
      queryClient.invalidateQueries({ queryKey: ['recurring-metrics'] });
      toast.success('Manual review resolved.');
      setReviewOrder(null);
      setReviewNotes('');
    },
    onError: (err) => toast.error(err.message || 'Failed to resolve review'),
  });

  const cancelOrderMutation = useMutation({
    mutationFn: (id) => recurringOrderService.cancelOrder(id, 'Cancelled by admin'),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['recurring-orders'] });
      queryClient.invalidateQueries({ queryKey: ['recurring-metrics'] });
      toast.success('Order cancelled.');
      setViewingOrder(null);
    },
    onError: (err) => toast.error(err.message || 'Failed to cancel order'),
  });

  const simulateReplyMutation = useMutation({
    mutationFn: (data) => recurringOrderService.simulateReply(data),
    onSuccess: (res) => {
      setSimResult(res);
      queryClient.invalidateQueries({ queryKey: ['recurring-orders'] });
      queryClient.invalidateQueries({ queryKey: ['recurring-metrics'] });
      toast.success(`AI Response: ${res.intent}`);
    },
    onError: (err) => toast.error(err.message || 'Simulation failed'),
  });

  const handleSimulate = (text) => {
    setSimBody(text);
  };

  const executeSimulation = (e) => {
    e.preventDefault();
    if (!simEmail.trim() || !simBody.trim()) {
      toast.error('Email and message body are required.');
      return;
    }
    simulateReplyMutation.mutate({
      fromEmail: simEmail.trim(),
      subject: 'Re: Your Upcoming Grocery Order',
      body: simBody.trim(),
    });
  };

  const columns = [
    {
      header: 'Order #',
      accessor: 'orderNumber',
      render: (num, row) => (
        <div>
          <button
            onClick={() => setViewingOrder(row)}
            className="font-bold text-blue-600 hover:underline"
          >
            {num}
          </button>
          <div className="text-[11px] text-slate-500">
            Source: <span className="font-semibold text-slate-700 dark:text-slate-300">{row.orderSourceName}</span>
          </div>
        </div>
      ),
    },
    {
      header: 'Customer',
      accessor: 'customerName',
      render: (_, row) => (
        <div>
          <button
            onClick={() => navigate(`/customers/${row.customerId}`)}
            className="font-semibold text-slate-900 dark:text-slate-100 hover:text-blue-600 transition"
          >
            {row.customerName}
          </button>
          <div className="text-xs text-slate-500">{row.customerEmail}</div>
        </div>
      ),
    },
    {
      header: 'Delivery Schedule',
      accessor: 'expectedDeliveryDate',
      render: (_, row) => (
        <div className="text-xs">
          <div className="font-semibold text-slate-800 dark:text-slate-200">
            {new Date(row.expectedDeliveryDate).toLocaleDateString()}
          </div>
          <div className="text-slate-500">{row.preferredDeliveryTime || 'Morning Slot'}</div>
        </div>
      ),
    },
    {
      header: 'Items',
      accessor: 'items',
      render: (items) => (
        <div className="text-xs font-semibold text-slate-800 dark:text-slate-200">
          {items?.length || 0} items
        </div>
      ),
    },
    {
      header: 'Amount',
      accessor: 'totalAmount',
      render: (amt) => (
        <span className="font-bold text-slate-900 dark:text-slate-100">
          ₹{amt.toFixed(2)}
        </span>
      ),
    },
    {
      header: 'Status & AI Checks',
      accessor: 'status',
      render: (_, row) => (
        <div className="space-y-1">
          <StatusBadge status={row.statusName} />
          {row.requiresManualReview && (
            <div>
              <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded text-[10px] font-bold bg-rose-100 text-rose-800 dark:bg-rose-900/50 dark:text-rose-200">
                <AlertTriangle className="w-3 h-3 text-rose-600" />
                Staff Review Needed
              </span>
            </div>
          )}
        </div>
      ),
    },
    {
      header: 'Actions',
      accessor: 'id',
      render: (_, row) => (
        <div className="flex items-center gap-1.5">
          <button
            onClick={() => setViewingOrder(row)}
            className="p-1.5 text-slate-500 hover:text-blue-600 hover:bg-blue-50 dark:hover:bg-blue-900/30 rounded"
            title="View Details"
          >
            <Eye className="w-4 h-4" />
          </button>

          {row.requiresManualReview && (
            <button
              onClick={() => {
                setReviewOrder(row);
                setReviewNotes('');
              }}
              className="px-2 py-1 text-xs font-semibold bg-rose-50 text-rose-700 border border-rose-200 hover:bg-rose-100 rounded"
            >
              Resolve Review
            </button>
          )}

          {row.status === 4 && ( // Approved
            <button
              onClick={() => finalizeToSaleMutation.mutate(row.id)}
              disabled={finalizeToSaleMutation.isPending}
              className="px-2.5 py-1 text-xs font-bold bg-emerald-600 hover:bg-emerald-700 text-white rounded transition shadow-sm"
              title="Convert approved order to real sale invoice and deduct stock"
            >
              Finalize Sale
            </button>
          )}
        </div>
      ),
    },
  ];

  return (
    <div className="space-y-6">
      <PageHeader
        title="AI-Powered Recurring Orders"
        subtitle="Automated draft generation, natural language customer approvals, and scheduled deliveries."
      >
        <div className="flex items-center gap-2">
          <button
            onClick={() => setIsSimulatorOpen(true)}
            className="flex items-center gap-2 px-3.5 py-2 border border-purple-300 dark:border-purple-700 text-purple-700 dark:text-purple-300 bg-purple-50/50 dark:bg-purple-950/20 hover:bg-purple-100/50 rounded-lg text-sm font-semibold transition"
          >
            <Sparkles className="w-4 h-4 text-purple-600" />
            Test AI Email Reply
          </button>

          <button
            onClick={() => triggerCycleMutation.mutate()}
            disabled={triggerCycleMutation.isPending}
            className="flex items-center gap-2 px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg text-sm font-semibold shadow-sm transition disabled:opacity-50"
          >
            {triggerCycleMutation.isPending ? (
              <Loader2 className="w-4 h-4 animate-spin" />
            ) : (
              <Play className="w-4 h-4" />
            )}
            Run Automation Cycle
          </button>
        </div>
      </PageHeader>

      {/* Metric Cards Row */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <div className="bg-white dark:bg-slate-800 p-4 rounded-xl border border-slate-200 dark:border-slate-700 shadow-sm">
          <div className="flex items-center justify-between text-slate-500 text-xs font-semibold uppercase">
            <span>Total Active Customers</span>
            <Users className="w-4 h-4 text-blue-500" />
          </div>
          <div className="text-2xl font-bold text-slate-900 dark:text-slate-100 mt-2">
            {metrics?.totalActiveCustomers ?? 0}
          </div>
          <div className="text-xs text-slate-500 mt-1">
            {metrics?.customersWithRecurringOrders ?? 0} with recurring enabled
          </div>
        </div>

        <div className="bg-white dark:bg-slate-800 p-4 rounded-xl border border-slate-200 dark:border-slate-700 shadow-sm">
          <div className="flex items-center justify-between text-slate-500 text-xs font-semibold uppercase">
            <span>Pending Approvals</span>
            <Clock className="w-4 h-4 text-amber-500" />
          </div>
          <div className="text-2xl font-bold text-amber-600 dark:text-amber-400 mt-2">
            {metrics?.pendingCustomerApprovals ?? 0}
          </div>
          <div className="text-xs text-slate-500 mt-1">
            Awaiting customer email / portal reply
          </div>
        </div>

        <div className="bg-white dark:bg-slate-800 p-4 rounded-xl border border-slate-200 dark:border-slate-700 shadow-sm">
          <div className="flex items-center justify-between text-slate-500 text-xs font-semibold uppercase">
            <span>Approved Orders</span>
            <CheckCircle2 className="w-4 h-4 text-emerald-500" />
          </div>
          <div className="text-2xl font-bold text-emerald-600 dark:text-emerald-400 mt-2">
            {metrics?.approvedOrders ?? 0}
          </div>
          <div className="text-xs text-slate-500 mt-1">
            Ready to finalize & dispatch
          </div>
        </div>

        <div
          className={`p-4 rounded-xl border shadow-sm transition ${
            (metrics?.ordersRequiringManualReview ?? 0) > 0
              ? 'bg-rose-50/60 border-rose-300 dark:bg-rose-950/20 dark:border-rose-800'
              : 'bg-white dark:bg-slate-800 border-slate-200 dark:border-slate-700'
          }`}
        >
          <div className="flex items-center justify-between text-xs font-semibold uppercase text-rose-600 dark:text-rose-400">
            <span>Staff Review Needed</span>
            <AlertTriangle className="w-4 h-4" />
          </div>
          <div className="text-2xl font-bold text-rose-600 dark:text-rose-400 mt-2">
            {metrics?.ordersRequiringManualReview ?? 0}
          </div>
          <div className="text-xs text-slate-500 mt-1">
            Low AI confidence or stock issues
          </div>
        </div>
      </div>

      {/* Filter and Search Bar */}
      <div className="bg-white dark:bg-slate-800 p-4 rounded-xl border border-slate-200 dark:border-slate-700 shadow-sm flex flex-col md:flex-row items-center justify-between gap-4">
        <div className="w-full md:w-80">
          <SearchInput
            value={search}
            onChange={(e) => {
              setSearch(e.target.value);
              setCurrentPage(1);
            }}
            placeholder="Search order #, customer..."
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
            <option value="">All Statuses</option>
            <option value="1">Draft</option>
            <option value="2">Pending Customer Approval</option>
            <option value="3">Modification Requested</option>
            <option value="4">Approved</option>
            <option value="5">Skipped</option>
            <option value="6">Confirmed</option>
            <option value="9">Cancelled</option>
          </select>

          {/* Frequency selector */}
          <select
            value={frequencyFilter}
            onChange={(e) => {
              setFrequencyFilter(e.target.value);
              setCurrentPage(1);
            }}
            className="px-3 py-2 text-sm border border-slate-200 dark:border-slate-700 rounded-lg bg-white dark:bg-slate-900 text-slate-800 dark:text-slate-200"
          >
            <option value="">All Frequencies</option>
            <option value="1">Every 7 Days</option>
            <option value="2">Every 15 Days</option>
            <option value="3">Every 30 Days</option>
            <option value="4">Every Month</option>
            <option value="5">Every 2 Months</option>
          </select>

          {/* Manual Review Only button */}
          <button
            onClick={() => {
              setManualReviewFilter(!manualReviewFilter);
              setCurrentPage(1);
            }}
            className={`flex items-center gap-1.5 px-3 py-2 text-sm rounded-lg border transition ${
              manualReviewFilter
                ? 'bg-rose-100 border-rose-300 text-rose-800 dark:bg-rose-900/40 dark:border-rose-700 dark:text-rose-300 font-semibold'
                : 'border-slate-200 dark:border-slate-700 text-slate-600 dark:text-slate-400 bg-white dark:bg-slate-900'
            }`}
          >
            <AlertTriangle className="w-4 h-4 text-rose-500" />
            Review Needed
          </button>
        </div>
      </div>

      {/* Orders Table */}
      <div className="bg-white dark:bg-slate-800 rounded-xl border border-slate-200 dark:border-slate-700 shadow-sm overflow-hidden">
        <DataTable
          columns={columns}
          data={orders}
          isLoading={isLoading}
          emptyMessage="No recurring orders found matching your filters."
        />

        {totalCount > pageSize && (
          <div className="p-4 border-t border-slate-200 dark:border-slate-700">
            <Pagination
              currentPage={currentPage}
              totalPages={totalPages}
              onPageChange={setCurrentPage}
            />
          </div>
        )}
      </div>

      {/* ORDER DETAILS MODAL */}
      {viewingOrder && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/50 backdrop-blur-sm">
          <div className="bg-white dark:bg-slate-800 rounded-2xl max-w-2xl w-full p-6 shadow-xl border border-slate-200 dark:border-slate-700 max-h-[90vh] overflow-y-auto space-y-4">
            <div className="flex items-center justify-between pb-3 border-b border-slate-200 dark:border-slate-700">
              <div>
                <h3 className="text-lg font-bold text-slate-900 dark:text-slate-100">
                  Recurring Order #{viewingOrder.orderNumber}
                </h3>
                <p className="text-xs text-slate-500">
                  Customer: {viewingOrder.customerName} ({viewingOrder.customerEmail})
                </p>
              </div>
              <button
                onClick={() => setViewingOrder(null)}
                className="text-slate-400 hover:text-slate-600"
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            <div className="grid grid-cols-2 gap-4 text-xs bg-slate-50 dark:bg-slate-900/50 p-3 rounded-xl border border-slate-100 dark:border-slate-700">
              <div>
                <span className="text-slate-500">Delivery Address:</span>
                <p className="font-semibold text-slate-800 dark:text-slate-200 mt-0.5">
                  {viewingOrder.deliveryAddress}
                </p>
              </div>
              <div>
                <span className="text-slate-500">Expected Delivery:</span>
                <p className="font-semibold text-slate-800 dark:text-slate-200 mt-0.5">
                  {new Date(viewingOrder.expectedDeliveryDate).toLocaleDateString()} (
                  {viewingOrder.preferredDeliveryTime || 'Morning Slot'})
                </p>
              </div>
            </div>

            {viewingOrder.specialInstructions && (
              <div className="text-xs bg-blue-50/50 dark:bg-blue-900/20 p-2.5 rounded-lg border border-blue-200 dark:border-blue-800 text-blue-900 dark:text-blue-200">
                <strong>Customer Instructions:</strong> {viewingOrder.specialInstructions}
              </div>
            )}

            {/* Items Table */}
            <div className="border border-slate-200 dark:border-slate-700 rounded-xl overflow-hidden">
              <table className="w-full text-left text-xs">
                <thead className="bg-slate-100 dark:bg-slate-900 font-semibold text-slate-600 dark:text-slate-400">
                  <tr>
                    <th className="p-2.5">Product</th>
                    <th className="p-2.5 text-center">Quantity</th>
                    <th className="p-2.5 text-right">Price</th>
                    <th className="p-2.5 text-right">Subtotal</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
                  {viewingOrder.items?.map((item) => (
                    <tr key={item.id}>
                      <td className="p-2.5">
                        <div className="font-semibold text-slate-800 dark:text-slate-200">
                          {item.productName}
                        </div>
                        {item.isModifiedByCustomer && (
                          <span className="text-[10px] text-amber-600">Modified by customer</span>
                        )}
                      </td>
                      <td className="p-2.5 text-center font-semibold">
                        {item.quantity} {item.unitName}
                      </td>
                      <td className="p-2.5 text-right">₹{item.unitPrice.toFixed(2)}</td>
                      <td className="p-2.5 text-right font-bold text-slate-900 dark:text-slate-100">
                        ₹{item.subTotal.toFixed(2)}
                      </td>
                    </tr>
                  ))}
                </tbody>
                <tfoot>
                  <tr className="bg-slate-50 dark:bg-slate-900/70 border-t border-slate-200 dark:border-slate-700 font-bold text-sm">
                    <td colSpan={3} className="p-3 text-right">
                      Grand Total:
                    </td>
                    <td className="p-3 text-right text-blue-600">
                      ₹{viewingOrder.totalAmount.toFixed(2)}
                    </td>
                  </tr>
                </tfoot>
              </table>
            </div>

            {/* Public Approval Portal Token Link */}
            <div className="text-xs bg-slate-50 dark:bg-slate-900 p-3 rounded-lg border border-slate-200 dark:border-slate-700">
              <span className="text-slate-500 font-semibold block mb-1">
                Customer Public Approval Portal URL:
              </span>
              <div className="flex items-center gap-2">
                <input
                  type="text"
                  readOnly
                  value={`${window.location.origin}/order-approval/${viewingOrder.approvalTokens?.[0]?.token || ''}`}
                  className="w-full text-xs bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded px-2 py-1 font-mono text-slate-600 dark:text-slate-400"
                />
                <button
                  onClick={() => {
                    const token = viewingOrder.approvalTokens?.[0]?.token;
                    if (token) {
                      window.open(`/order-approval/${token}`, '_blank');
                    } else {
                      toast.error('Token not found');
                    }
                  }}
                  className="px-3 py-1 bg-slate-200 dark:bg-slate-700 hover:bg-slate-300 rounded font-semibold text-xs flex items-center gap-1"
                >
                  <ExternalLink className="w-3.5 h-3.5" />
                  Open
                </button>
              </div>
            </div>

            {/* Modal Bottom Actions */}
            <div className="flex justify-between items-center pt-3 border-t border-slate-200 dark:border-slate-700">
              <button
                type="button"
                onClick={() => cancelOrderMutation.mutate(viewingOrder.id)}
                className="text-xs text-rose-600 hover:underline font-semibold"
              >
                Cancel Order
              </button>

              <div className="flex gap-2">
                <button
                  type="button"
                  onClick={() => setViewingOrder(null)}
                  className="px-4 py-2 border border-slate-200 dark:border-slate-700 text-slate-700 dark:text-slate-300 rounded-lg text-xs font-semibold"
                >
                  Close
                </button>

                {viewingOrder.status === 4 && ( // Approved
                  <button
                    type="button"
                    onClick={() => finalizeToSaleMutation.mutate(viewingOrder.id)}
                    disabled={finalizeToSaleMutation.isPending}
                    className="px-4 py-2 bg-emerald-600 hover:bg-emerald-700 text-white rounded-lg text-xs font-bold transition shadow-sm"
                  >
                    Confirm & Finalize Sale
                  </button>
                )}
              </div>
            </div>
          </div>
        </div>
      )}

      {/* RESOLVE MANUAL REVIEW MODAL */}
      {reviewOrder && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/50 backdrop-blur-sm">
          <div className="bg-white dark:bg-slate-800 rounded-2xl max-w-lg w-full p-6 shadow-xl border border-slate-200 dark:border-slate-700 space-y-4">
            <div className="flex items-center justify-between pb-3 border-b border-slate-200 dark:border-slate-700">
              <h3 className="text-base font-bold text-rose-600 flex items-center gap-2">
                <AlertTriangle className="w-5 h-5" />
                Resolve Staff Review for Order #{reviewOrder.orderNumber}
              </h3>
              <button
                onClick={() => setReviewOrder(null)}
                className="text-slate-400 hover:text-slate-600"
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            <div className="bg-rose-50 dark:bg-rose-950/30 p-3 rounded-lg border border-rose-200 dark:border-rose-800 text-xs text-rose-900 dark:text-rose-200">
              <strong>Flag Reason:</strong> {reviewOrder.manualReviewReason || 'Low AI confidence or inventory discrepancy.'}
            </div>

            <div>
              <label className="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">
                Admin Resolution Notes
              </label>
              <textarea
                rows={3}
                value={reviewNotes}
                onChange={(e) => setReviewNotes(e.target.value)}
                placeholder="e.g. Manually verified stock with shop assistant and confirmed order."
                className="w-full px-3 py-2 text-xs border border-slate-200 dark:border-slate-700 rounded-lg bg-white dark:bg-slate-900"
              />
            </div>

            <div className="flex justify-end gap-3 pt-3 border-t border-slate-200 dark:border-slate-700">
              <button
                type="button"
                onClick={() =>
                  resolveReviewMutation.mutate({
                    id: reviewOrder.id,
                    approve: false,
                    notes: reviewNotes,
                  })
                }
                className="px-4 py-2 border border-rose-300 text-rose-700 hover:bg-rose-50 rounded-lg text-xs font-semibold"
              >
                Reject / Cancel Order
              </button>
              <button
                type="button"
                onClick={() =>
                  resolveReviewMutation.mutate({
                    id: reviewOrder.id,
                    approve: true,
                    notes: reviewNotes,
                  })
                }
                className="px-4 py-2 bg-emerald-600 hover:bg-emerald-700 text-white rounded-lg text-xs font-bold"
              >
                Approve Order
              </button>
            </div>
          </div>
        </div>
      )}

      {/* AI NATURAL LANGUAGE SIMULATOR MODAL */}
      {isSimulatorOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/50 backdrop-blur-sm">
          <div className="bg-white dark:bg-slate-800 rounded-2xl max-w-xl w-full p-6 shadow-xl border border-slate-200 dark:border-slate-700 max-h-[90vh] overflow-y-auto space-y-4">
            <div className="flex items-center justify-between pb-3 border-b border-slate-200 dark:border-slate-700">
              <div className="flex items-center gap-2">
                <Sparkles className="w-5 h-5 text-purple-600" />
                <h3 className="text-base font-bold text-slate-900 dark:text-slate-100">
                  AI Natural Language Email Reply Simulator
                </h3>
              </div>
              <button
                onClick={() => {
                  setIsSimulatorOpen(false);
                  setSimResult(null);
                }}
                className="text-slate-400 hover:text-slate-600"
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            <p className="text-xs text-slate-500">
              Simulate an incoming email reply from a customer. The AI engine classifies intent, validates inventory stock, checks safety constraints, and updates the order status.
            </p>

            {/* Quick Preset Buttons */}
            <div>
              <span className="text-[11px] font-semibold text-slate-500 uppercase block mb-1.5">
                Quick Test Examples:
              </span>
              <div className="flex flex-wrap gap-1.5">
                {[
                  'Yes, send me the same order.',
                  'Send 15 kg rice instead of 10 kg and 3 litres oil.',
                  'Please don\'t send anything this time.',
                  'Send it next Monday instead.',
                  'Please increase my regular rice quantity to 15 kg for all future orders.',
                ].map((preset, idx) => (
                  <button
                    key={idx}
                    type="button"
                    onClick={() => handleSimulate(preset)}
                    className="text-[11px] px-2.5 py-1 rounded bg-slate-100 dark:bg-slate-700 hover:bg-purple-100 dark:hover:bg-purple-900/40 text-slate-700 dark:text-slate-300 transition text-left"
                  >
                    "{preset}"
                  </button>
                ))}
              </div>
            </div>

            <form onSubmit={executeSimulation} className="space-y-3 pt-2">
              <div>
                <label className="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">
                  Customer Email (must match a registered customer) *
                </label>
                <input
                  type="email"
                  required
                  placeholder="e.g. rahul@example.com"
                  value={simEmail}
                  onChange={(e) => setSimEmail(e.target.value)}
                  className="w-full px-3 py-2 text-xs border border-slate-200 dark:border-slate-700 rounded-lg bg-white dark:bg-slate-900"
                />
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">
                  Incoming Email Body (Natural Language) *
                </label>
                <textarea
                  rows={3}
                  required
                  value={simBody}
                  onChange={(e) => setSimBody(e.target.value)}
                  placeholder="e.g. Yes, please send me the same order."
                  className="w-full px-3 py-2 text-xs border border-slate-200 dark:border-slate-700 rounded-lg bg-white dark:bg-slate-900"
                />
              </div>

              <div className="flex justify-end gap-2 pt-2">
                <button
                  type="submit"
                  disabled={simulateReplyMutation.isPending}
                  className="flex items-center gap-2 px-5 py-2 bg-purple-600 hover:bg-purple-700 text-white rounded-lg text-xs font-bold transition disabled:opacity-50"
                >
                  {simulateReplyMutation.isPending && (
                    <Loader2 className="w-4 h-4 animate-spin" />
                  )}
                  Process with AI
                </button>
              </div>
            </form>

            {/* Simulation Result Output */}
            {simResult && (
              <div className="mt-4 p-4 rounded-xl border border-purple-200 dark:border-purple-800 bg-purple-50/50 dark:bg-purple-950/20 space-y-2 text-xs">
                <div className="flex items-center justify-between font-bold text-purple-900 dark:text-purple-200">
                  <span>Classified Intent: {simResult.intent}</span>
                  <span className="px-2 py-0.5 rounded bg-purple-200 dark:bg-purple-800 text-[10px]">
                    {(simResult.confidence * 100).toFixed(0)}% Confidence
                  </span>
                </div>
                <div className="text-slate-700 dark:text-slate-300">
                  <strong>Action Taken:</strong> {simResult.actionTaken}
                </div>
                <div className="text-slate-700 dark:text-slate-300">
                  <strong>Automated Reply Sent to Customer:</strong>
                  <p className="p-2 mt-1 bg-white dark:bg-slate-900 rounded border border-purple-100 dark:border-purple-900 italic">
                    "{simResult.customerMessage}"
                  </p>
                </div>
                {simResult.requiresManualReview && (
                  <div className="text-rose-600 font-semibold text-[11px]">
                    ⚠️ Flagged for manual review: {simResult.manualReviewReason}
                  </div>
                )}
                {simResult.subscriptionChange && (
                  <div className="text-amber-700 font-semibold text-[11px]">
                    📌 Customer requested a permanent recurring subscription change. Requires explicit review.
                  </div>
                )}
              </div>
            )}
          </div>
        </div>
      )}
    </div>
  );
}
