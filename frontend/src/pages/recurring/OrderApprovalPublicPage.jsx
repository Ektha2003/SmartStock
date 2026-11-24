import React, { useState, useEffect } from 'react';
import { useParams } from 'react-router-dom';
import { useQuery, useMutation } from '@tanstack/react-query';
import {
  ShoppingBag,
  CheckCircle2,
  Calendar,
  MapPin,
  Clock,
  Plus,
  Trash2,
  AlertCircle,
  Loader2,
  XCircle,
  Store,
  ChevronRight,
  ShieldCheck,
} from 'lucide-react';
import recurringOrderService from '../../services/recurringOrderService';
import { toast } from 'sonner';

export default function OrderApprovalPublicPage() {
  const { token } = useParams();

  const [items, setItems] = useState([]);
  const [requestedDeliveryDate, setRequestedDeliveryDate] = useState('');
  const [specialInstructions, setSpecialInstructions] = useState('');
  const [selectedCatalogProductId, setSelectedCatalogProductId] = useState('');
  const [isModified, setIsModified] = useState(false);
  const [showSkipConfirm, setShowSkipConfirm] = useState(false);

  // Fetch Order by token (Public endpoint, no auth required)
  const {
    data: order,
    isLoading,
    isError,
    error,
    refetch,
  } = useQuery({
    queryKey: ['portal-order', token],
    queryFn: () => recurringOrderService.getOrderByToken(token),
    retry: 1,
  });

  // Sync state when order loads
  useEffect(() => {
    if (order) {
      setItems(
        (order.items || []).map((it) => ({
          productId: it.productId,
          productName: it.productName,
          quantity: it.quantity,
          unitName: it.unitName,
          unitPrice: it.unitPrice,
          subTotal: it.subTotal,
          productInstructions: it.productInstructions || '',
        }))
      );
      setRequestedDeliveryDate(
        order.expectedDeliveryDate ? order.expectedDeliveryDate.substring(0, 10) : ''
      );
      setSpecialInstructions(order.specialInstructions || '');
      setIsModified(false);
    }
  }, [order]);

  // Mutation to submit decision
  const submitMutation = useMutation({
    mutationFn: (data) => recurringOrderService.submitOrderDecision(token, data),
    onSuccess: (updated) => {
      toast.success(
        updated.status === 5
          ? 'Delivery skipped for this cycle.'
          : 'Thank you! Your order has been confirmed.'
      );
      refetch();
    },
    onError: (err) => {
      toast.error(err.message || 'Failed to submit order');
    },
  });

  if (isLoading) {
    return (
      <div className="min-h-screen bg-slate-50 flex flex-col items-center justify-center p-4">
        <Loader2 className="w-10 h-10 text-blue-600 animate-spin mb-3" />
        <p className="text-slate-600 font-medium">Loading your grocery order...</p>
      </div>
    );
  }

  if (isError || !order) {
    return (
      <div className="min-h-screen bg-slate-50 flex items-center justify-center p-4">
        <div className="bg-white max-w-md w-full p-8 rounded-2xl shadow-sm border border-slate-200 text-center space-y-4">
          <div className="w-12 h-12 bg-rose-100 text-rose-600 rounded-full flex items-center justify-center mx-auto">
            <XCircle className="w-6 h-6" />
          </div>
          <h2 className="text-xl font-bold text-slate-900">Link Invalid or Expired</h2>
          <p className="text-sm text-slate-600">
            {error?.message ||
              'This order confirmation link is invalid or has expired. Please contact the store or wait for the next scheduled reminder.'}
          </p>
        </div>
      </div>
    );
  }

  // Handle quantity change
  const handleQuantityChange = (productId, newQty) => {
    const qty = parseFloat(newQty);
    if (isNaN(qty) || qty < 0) return;

    setItems((prev) =>
      prev.map((it) => {
        if (it.productId === productId) {
          return {
            ...it,
            quantity: qty,
            subTotal: Math.round(qty * it.unitPrice * 100) / 100,
          };
        }
        return it;
      })
    );
    setIsModified(true);
  };

  // Remove item
  const handleRemoveItem = (productId) => {
    setItems((prev) => prev.filter((it) => it.productId !== productId));
    setIsModified(true);
  };

  // Add catalog item
  const handleAddCatalogItem = () => {
    if (!selectedCatalogProductId) return;
    const catItem = (order.availableCatalog || []).find((p) => p.id === selectedCatalogProductId);
    if (!catItem) return;

    if (items.some((i) => i.productId === catItem.id)) {
      toast.info('Item is already in your order. You can increase its quantity.');
      return;
    }

    setItems((prev) => [
      ...prev,
      {
        productId: catItem.id,
        productName: catItem.name,
        quantity: 1,
        unitName: catItem.unitName,
        unitPrice: catItem.sellingPrice,
        subTotal: catItem.sellingPrice,
        productInstructions: '',
      },
    ]);
    setSelectedCatalogProductId('');
    setIsModified(true);
  };

  const calculatedTotal = items.reduce((sum, it) => sum + (it.subTotal || 0), 0);

  // Submit complete approval
  const handleApproveAsIs = () => {
    submitMutation.mutate({
      action: 'APPROVE',
      specialInstructions: specialInstructions.trim(),
    });
  };

  // Submit modifications
  const handleConfirmModified = () => {
    if (items.length === 0) {
      toast.error('Your order has no items. Please select "Skip Delivery" if you wish to cancel this delivery.');
      return;
    }

    submitMutation.mutate({
      action: 'MODIFY',
      requestedDeliveryDate: requestedDeliveryDate ? new Date(requestedDeliveryDate).toISOString() : null,
      specialInstructions: specialInstructions.trim(),
      items: items.map((it) => ({
        productId: it.productId,
        quantity: it.quantity,
        productInstructions: it.productInstructions,
      })),
    });
  };

  // Submit skip
  const handleSkipOrder = () => {
    submitMutation.mutate({
      action: 'SKIP',
    });
    setShowSkipConfirm(false);
  };

  const isEditable = order.isEditable;
  const isApproved = order.status === 4; // Approved
  const isConfirmed = order.status === 6; // Confirmed
  const isSkipped = order.status === 5; // Skipped

  return (
    <div className="min-h-screen bg-slate-100/70 py-8 px-4 sm:px-6">
      <div className="max-w-2xl mx-auto space-y-6">
        {/* Brand Banner */}
        <div className="bg-white rounded-2xl shadow-sm border border-slate-200 p-6 flex items-center justify-between">
          <div className="flex items-center gap-3">
            <div className="h-12 w-12 rounded-xl bg-blue-600 text-white flex items-center justify-center font-bold text-lg shadow-sm">
              <Store className="w-6 h-6" />
            </div>
            <div>
              <h1 className="text-xl font-bold text-slate-900">SmartStock Grocery Delivery</h1>
              <p className="text-xs text-slate-500">Scheduled Recurring Delivery Confirmation</p>
            </div>
          </div>
          <div className="text-right">
            <span className="text-xs font-semibold text-slate-500 block">Order Reference</span>
            <span className="font-mono text-sm font-bold text-blue-600">{order.orderNumber}</span>
          </div>
        </div>

        {/* Status Callout if already processed */}
        {isApproved && (
          <div className="bg-emerald-50 border border-emerald-200 rounded-2xl p-5 flex items-center gap-4 text-emerald-900">
            <CheckCircle2 className="w-8 h-8 text-emerald-600 shrink-0" />
            <div>
              <h3 className="font-bold text-base">Order Approved & Confirmed!</h3>
              <p className="text-xs text-emerald-700 mt-0.5">
                Thank you, {order.customerName}! Your regular order is scheduled for delivery on{' '}
                <strong>{new Date(order.expectedDeliveryDate).toLocaleDateString()}</strong>.
              </p>
            </div>
          </div>
        )}

        {isConfirmed && (
          <div className="bg-emerald-50 border border-emerald-200 rounded-2xl p-5 flex items-center gap-4 text-emerald-900">
            <CheckCircle2 className="w-8 h-8 text-emerald-600 shrink-0" />
            <div>
              <h3 className="font-bold text-base">Invoice Generated & Packed!</h3>
              <p className="text-xs text-emerald-700 mt-0.5">
                Your order #{order.orderNumber} is confirmed and packed for delivery to{' '}
                <strong>{order.deliveryAddress}</strong>.
              </p>
            </div>
          </div>
        )}

        {isSkipped && (
          <div className="bg-slate-50 border border-slate-200 rounded-2xl p-5 flex items-center gap-4 text-slate-700">
            <Clock className="w-8 h-8 text-slate-500 shrink-0" />
            <div>
              <h3 className="font-bold text-base">Delivery Skipped</h3>
              <p className="text-xs text-slate-500 mt-0.5">
                You have skipped this delivery cycle. Your subscription remains active for future scheduled dates.
              </p>
            </div>
          </div>
        )}

        {/* Customer & Delivery Information Card */}
        <div className="bg-white rounded-2xl shadow-sm border border-slate-200 p-6 space-y-4">
          <h2 className="text-sm font-bold text-slate-900 uppercase tracking-wider text-slate-500">
            Delivery Details
          </h2>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 text-sm">
            <div className="flex items-start gap-3">
              <MapPin className="w-4 h-4 text-blue-600 shrink-0 mt-1" />
              <div>
                <span className="text-xs text-slate-500 font-medium">Deliver To:</span>
                <p className="font-semibold text-slate-800">{order.customerName}</p>
                <p className="text-xs text-slate-600 mt-0.5">{order.deliveryAddress}</p>
              </div>
            </div>

            <div className="flex items-start gap-3">
              <Calendar className="w-4 h-4 text-blue-600 shrink-0 mt-1" />
              <div>
                <span className="text-xs text-slate-500 font-medium">Scheduled Delivery Date:</span>
                {isEditable ? (
                  <input
                    type="date"
                    value={requestedDeliveryDate}
                    onChange={(e) => {
                      setRequestedDeliveryDate(e.target.value);
                      setIsModified(true);
                    }}
                    className="mt-1 block text-xs border border-slate-200 rounded-lg px-2.5 py-1.5 font-medium text-slate-800 bg-slate-50"
                  />
                ) : (
                  <p className="font-semibold text-slate-800">
                    {new Date(order.expectedDeliveryDate).toLocaleDateString()}
                  </p>
                )}
                <span className="text-xs text-slate-500 mt-0.5 block">
                  Slot: {order.preferredDeliveryTime || 'Morning 09:00 - 12:00'}
                </span>
              </div>
            </div>
          </div>

          <div>
            <label className="block text-xs text-slate-500 font-medium mb-1">
              Delivery Notes / Instructions:
            </label>
            {isEditable ? (
              <input
                type="text"
                placeholder="e.g. Ring bell twice, leave at door..."
                value={specialInstructions}
                onChange={(e) => {
                  setSpecialInstructions(e.target.value);
                  setIsModified(true);
                }}
                className="w-full text-xs border border-slate-200 rounded-lg px-3 py-2 bg-slate-50"
              />
            ) : (
              <p className="text-xs text-slate-700 italic">
                {order.specialInstructions || 'None specified'}
              </p>
            )}
          </div>
        </div>

        {/* Order Basket Items Card */}
        <div className="bg-white rounded-2xl shadow-sm border border-slate-200 p-6 space-y-4">
          <div className="flex items-center justify-between">
            <h2 className="text-sm font-bold text-slate-900 uppercase tracking-wider text-slate-500">
              Your Scheduled Basket
            </h2>
            {isEditable && (
              <span className="text-xs text-slate-500">
                You can adjust quantities or remove items below
              </span>
            )}
          </div>

          <div className="divide-y divide-slate-100">
            {items.map((item) => (
              <div
                key={item.productId}
                className="py-3.5 flex flex-col sm:flex-row items-start sm:items-center justify-between gap-3"
              >
                <div className="flex-1">
                  <div className="font-semibold text-sm text-slate-900">{item.productName}</div>
                  <div className="text-xs text-slate-500">
                    ₹{item.unitPrice.toFixed(2)} per {item.unitName}
                  </div>
                </div>

                <div className="flex items-center gap-4 w-full sm:w-auto justify-between sm:justify-end">
                  {isEditable ? (
                    <div className="flex items-center gap-2">
                      <input
                        type="number"
                        min="0"
                        step="0.5"
                        value={item.quantity}
                        onChange={(e) => handleQuantityChange(item.productId, e.target.value)}
                        className="w-20 px-2 py-1 text-center font-bold text-sm border border-slate-200 rounded-lg bg-slate-50"
                      />
                      <span className="text-xs text-slate-600 font-medium">{item.unitName}</span>
                    </div>
                  ) : (
                    <div className="font-bold text-sm text-slate-800">
                      {item.quantity} {item.unitName}
                    </div>
                  )}

                  <div className="text-right min-w-[70px]">
                    <span className="font-bold text-sm text-slate-900">
                      ₹{item.subTotal.toFixed(2)}
                    </span>
                  </div>

                  {isEditable && (
                    <button
                      type="button"
                      onClick={() => handleRemoveItem(item.productId)}
                      className="p-1 text-slate-400 hover:text-rose-600 rounded"
                      title="Remove from this delivery"
                    >
                      <Trash2 className="w-4 h-4" />
                    </button>
                  )}
                </div>
              </div>
            ))}
          </div>

          {/* Add extra products from catalog if editable */}
          {isEditable && order.availableCatalog && order.availableCatalog.length > 0 && (
            <div className="pt-4 border-t border-slate-100 flex flex-col sm:flex-row items-center gap-2">
              <select
                value={selectedCatalogProductId}
                onChange={(e) => setSelectedCatalogProductId(e.target.value)}
                className="w-full sm:flex-1 text-xs border border-slate-200 rounded-lg px-3 py-2 bg-slate-50"
              >
                <option value="">+ Add another item from store catalog...</option>
                {order.availableCatalog.map((cat) => (
                  <option key={cat.id} value={cat.id}>
                    {cat.name} — ₹{cat.sellingPrice} per {cat.unitName}
                  </option>
                ))}
              </select>
              <button
                type="button"
                onClick={handleAddCatalogItem}
                disabled={!selectedCatalogProductId}
                className="w-full sm:w-auto px-4 py-2 bg-slate-800 text-white rounded-lg text-xs font-semibold hover:bg-slate-900 disabled:opacity-40"
              >
                Add Item
              </button>
            </div>
          )}

          {/* Grand Total */}
          <div className="pt-4 border-t-2 border-slate-100 flex items-center justify-between">
            <span className="text-base font-bold text-slate-900">Grand Total:</span>
            <span className="text-2xl font-bold text-blue-600">
              ₹{calculatedTotal.toFixed(2)}
            </span>
          </div>
        </div>

        {/* Action Buttons (when editable) */}
        {isEditable && (
          <div className="space-y-3">
            <div className="flex flex-col sm:flex-row gap-3">
              {isModified ? (
                <button
                  type="button"
                  onClick={handleConfirmModified}
                  disabled={submitMutation.isPending}
                  className="flex-1 flex items-center justify-center gap-2 py-3.5 px-6 bg-blue-600 hover:bg-blue-700 text-white rounded-xl font-bold text-sm shadow-md transition disabled:opacity-50"
                >
                  {submitMutation.isPending && <Loader2 className="w-4 h-4 animate-spin" />}
                  Submit Modified Order (₹{calculatedTotal.toFixed(2)})
                </button>
              ) : (
                <button
                  type="button"
                  onClick={handleApproveAsIs}
                  disabled={submitMutation.isPending}
                  className="flex-1 flex items-center justify-center gap-2 py-3.5 px-6 bg-emerald-600 hover:bg-emerald-700 text-white rounded-xl font-bold text-sm shadow-md transition disabled:opacity-50"
                >
                  {submitMutation.isPending && <Loader2 className="w-4 h-4 animate-spin" />}
                  <CheckCircle2 className="w-5 h-5" />
                  Approve Order as Scheduled (₹{calculatedTotal.toFixed(2)})
                </button>
              )}

              <button
                type="button"
                onClick={() => setShowSkipConfirm(true)}
                disabled={submitMutation.isPending}
                className="py-3.5 px-5 border border-slate-300 bg-white hover:bg-slate-50 text-slate-700 rounded-xl font-semibold text-sm transition"
              >
                Skip This Delivery
              </button>
            </div>

            <p className="text-center text-xs text-slate-500">
              You can also reply directly to the confirmation email from your inbox at any time.
            </p>
          </div>
        )}

        {/* Skip Confirmation Dialog */}
        {showSkipConfirm && (
          <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/50 backdrop-blur-sm">
            <div className="bg-white rounded-2xl max-w-sm w-full p-6 shadow-xl space-y-4 text-center">
              <Clock className="w-10 h-10 text-amber-500 mx-auto" />
              <h3 className="text-base font-bold text-slate-900">Skip This Delivery?</h3>
              <p className="text-xs text-slate-600">
                Are you sure you want to skip your scheduled grocery delivery for{' '}
                <strong>{new Date(order.expectedDeliveryDate).toLocaleDateString()}</strong>? No order
                will be sent and your account will not be charged.
              </p>
              <div className="flex gap-2 pt-2">
                <button
                  type="button"
                  onClick={() => setShowSkipConfirm(false)}
                  className="flex-1 py-2 text-xs font-semibold border border-slate-200 rounded-lg"
                >
                  Never mind
                </button>
                <button
                  type="button"
                  onClick={handleSkipOrder}
                  className="flex-1 py-2 text-xs font-bold bg-rose-600 text-white rounded-lg hover:bg-rose-700"
                >
                  Yes, Skip Delivery
                </button>
              </div>
            </div>
          </div>
        )}

        {/* Security badge */}
        <div className="flex items-center justify-center gap-1.5 text-xs text-slate-400">
          <ShieldCheck className="w-4 h-4 text-slate-400" />
          <span>Secure SmartStock Token-Verified Delivery Portal</span>
        </div>
      </div>
    </div>
  );
}
