import React, { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  Search,
  Plus,
  Minus,
  Trash2,
  ShoppingCart,
  ArrowLeft,
  CheckCircle2,
  Loader2,
} from 'lucide-react';
import productService from '../../services/productService';
import salesService from '../../services/salesService';
import PageHeader from '../../components/common/PageHeader';
import { formatCurrency } from '../../utils/formatters';
import { toast } from 'sonner';

export function NewSalePage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const [searchTerm, setSearchTerm] = useState('');
  const [cartItems, setCartItems] = useState([]);
  const [customerName, setCustomerName] = useState('');
  const [customerPhone, setCustomerPhone] = useState('');
  const [paymentMethod, setPaymentMethod] = useState('UPI');
  const [notes, setNotes] = useState('Cash counter checkout');

  const { data: productsData, isLoading: isProductsLoading } = useQuery({
    queryKey: ['products', 'active'],
    queryFn: () => productService.getProducts({ isActive: true, pageSize: 100 }),
  });

  const products = productsData?.items || (Array.isArray(productsData) ? productsData : []);

  const saleMutation = useMutation({
    mutationFn: (saleData) => salesService.createSale(saleData),
    onSuccess: (newSale) => {
      queryClient.invalidateQueries({ queryKey: ['sales'] });
      queryClient.invalidateQueries({ queryKey: ['products'] });
      queryClient.invalidateQueries({ queryKey: ['inventory'] });
      queryClient.invalidateQueries({ queryKey: ['dashboard'] });
      queryClient.invalidateQueries({ queryKey: ['notifications'] });
      toast.success(`Sale recorded! Invoice #${newSale.invoiceNumber}`);
      navigate(`/sales/${newSale.id || newSale.invoiceNumber}`);
    },
    onError: (err) => {
      toast.error(err.message || 'Failed to complete sale');
    },
  });

  // Filter products for quick selection search
  const availableProducts = products.filter((p) => {
    if (!searchTerm.trim()) return true;
    const term = searchTerm.toLowerCase();
    return p.name.toLowerCase().includes(term) || p.sku.toLowerCase().includes(term);
  });

  const addToCart = (product) => {
    if (product.currentStock <= 0) {
      toast.error(`${product.name} is out of stock!`);
      return;
    }

    const existingIndex = cartItems.findIndex((item) => String(item.productId) === String(product.id));
    if (existingIndex > -1) {
      const currentQty = cartItems[existingIndex].quantity;
      if (currentQty >= product.currentStock) {
        toast.error(`Only ${product.currentStock} ${product.unitName || 'units'} available in stock!`);
        return;
      }
      const updated = [...cartItems];
      updated[existingIndex].quantity += 1;
      setCartItems(updated);
    } else {
      setCartItems([
        ...cartItems,
        {
          productId: product.id,
          productName: product.name,
          sku: product.sku,
          unitPrice: product.sellingPrice,
          quantity: 1,
          availableStock: product.currentStock,
          unit: product.unitName || 'Piece',
        },
      ]);
    }
  };

  const updateQuantity = (productId, delta) => {
    setCartItems((prev) =>
      prev
        .map((item) => {
          if (String(item.productId) === String(productId)) {
            const nextQty = item.quantity + delta;
            if (nextQty > item.availableStock) {
              toast.error(`Cannot exceed available stock (${item.availableStock})`);
              return item;
            }
            return nextQty > 0 ? { ...item, quantity: nextQty } : null;
          }
          return item;
        })
        .filter(Boolean)
    );
  };

  const removeFromCart = (productId) => {
    setCartItems((prev) => prev.filter((item) => String(item.productId) !== String(productId)));
  };

  const subtotal = cartItems.reduce((sum, item) => sum + item.quantity * item.unitPrice, 0);

  const handleSubmit = (e) => {
    e.preventDefault();
    if (cartItems.length === 0) {
      toast.error('Please add at least one product to the sale');
      return;
    }

    for (const item of cartItems) {
      if (item.quantity > item.availableStock) {
        toast.error(`Insufficient stock for ${item.productName}`);
        return;
      }
    }

    // Backend POST /api/v1/sales payload
    saleMutation.mutate({
      saleDate: new Date().toISOString(),
      customerName: customerName.trim() || 'Walk-in Customer',
      customerPhone: customerPhone.trim(),
      paymentMethod,
      notes: notes.trim(),
      items: cartItems.map((item) => ({
        productId: item.productId,
        quantity: Number(item.quantity),
        unitPrice: Number(item.unitPrice),
      })),
    });
  };

  return (
    <div className="space-y-5">
      <PageHeader
        title="Point of Sale (POS) Billing Counter"
        subtitle="Quick counter checkout for Kirana customers with automatic stock deduction."
        breadcrumbs={[
          { label: 'Sales', to: '/sales' },
          { label: 'New Sale' },
        ]}
        action={
          <Link
            to="/sales"
            className="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold text-slate-700 bg-white border border-slate-200 rounded-lg hover:bg-slate-50 transition"
          >
            <ArrowLeft className="w-3.5 h-3.5" /> Back to Invoices
          </Link>
        }
      />

      <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">
        {/* Left Side: Product Selector (7 cols) */}
        <div className="lg:col-span-7 space-y-4">
          <div className="bg-white p-4 rounded-xl border border-slate-200 shadow-xs">
            <div className="relative">
              <Search className="w-4 h-4 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2" />
              <input
                type="text"
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                placeholder="Search products by name or SKU..."
                className="w-full pl-9 pr-3 py-2 text-sm bg-slate-50 border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-600 transition"
              />
            </div>
          </div>

          <div className="bg-white rounded-xl border border-slate-200 p-4 shadow-xs">
            <h4 className="text-xs font-semibold text-slate-500 uppercase tracking-wider mb-3">
              Store Catalog ({availableProducts.length} items available)
            </h4>

            {isProductsLoading ? (
              <div className="py-12 flex justify-center">
                <Loader2 className="w-6 h-6 animate-spin text-indigo-600" />
              </div>
            ) : availableProducts.length === 0 ? (
              <p className="text-center py-8 text-xs text-slate-500">No products match &quot;{searchTerm}&quot;</p>
            ) : (
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 max-h-[500px] overflow-y-auto pr-1">
                {availableProducts.map((p) => {
                  const isOut = p.currentStock <= 0;
                  const minStock = p.minimumStock ?? p.minStock ?? 5;
                  return (
                    <div
                      key={p.id}
                      onClick={() => !isOut && addToCart(p)}
                      className={`p-3 rounded-xl border transition flex flex-col justify-between cursor-pointer ${
                        isOut
                          ? 'bg-slate-50 border-slate-200 opacity-60 cursor-not-allowed'
                          : 'bg-white border-slate-200 hover:border-indigo-500 hover:shadow-xs'
                      }`}
                    >
                      <div>
                        <div className="flex items-start justify-between gap-1 mb-1">
                          <p className="font-semibold text-xs text-slate-900 line-clamp-2">{p.name}</p>
                        </div>
                        <p className="text-[11px] text-slate-400 font-mono">{p.sku}</p>
                      </div>

                      <div className="mt-3 pt-2 border-t border-slate-100 flex items-center justify-between">
                        <span className="font-bold text-slate-900 text-sm">{formatCurrency(p.sellingPrice)}</span>
                        <span
                          className={`text-[11px] font-semibold px-2 py-0.5 rounded-full ${
                            isOut
                              ? 'bg-rose-50 text-rose-700'
                              : p.currentStock <= minStock
                              ? 'bg-amber-50 text-amber-700'
                              : 'bg-emerald-50 text-emerald-700'
                          }`}
                        >
                          {isOut ? 'Out' : `${p.currentStock} ${p.unitName || ''}`}
                        </span>
                      </div>
                    </div>
                  );
                })}
              </div>
            )}
          </div>
        </div>

        {/* Right Side: Current Bill / Cart (5 cols) */}
        <div className="lg:col-span-5 space-y-4">
          <form onSubmit={handleSubmit} className="bg-white rounded-xl border border-slate-200 p-5 shadow-xs flex flex-col justify-between">
            <div>
              <div className="flex items-center justify-between pb-3 border-b border-slate-100 mb-4">
                <div className="flex items-center gap-2">
                  <ShoppingCart className="w-5 h-5 text-indigo-600" />
                  <h3 className="font-bold text-base text-slate-900">Current Cart</h3>
                </div>
                <span className="text-xs font-semibold px-2 py-0.5 bg-slate-100 text-slate-700 rounded-full">
                  {cartItems.length} items
                </span>
              </div>

              {/* Cart item list */}
              {cartItems.length === 0 ? (
                <div className="py-12 text-center text-slate-400">
                  <ShoppingCart className="w-10 h-10 mx-auto mb-2 opacity-30" />
                  <p className="text-sm font-medium">Cart is empty</p>
                  <p className="text-xs">Select items from catalog on left to add to bill.</p>
                </div>
              ) : (
                <div className="space-y-3 max-h-[280px] overflow-y-auto mb-4 pr-1 divide-y divide-slate-100">
                  {cartItems.map((item) => (
                    <div key={item.productId} className="pt-2 flex items-center justify-between text-xs">
                      <div className="truncate mr-2 flex-1">
                        <p className="font-semibold text-slate-900 truncate">{item.productName}</p>
                        <p className="text-slate-500">
                          {formatCurrency(item.unitPrice)} × {item.quantity} ={' '}
                          <span className="font-bold text-slate-800">
                            {formatCurrency(item.unitPrice * item.quantity)}
                          </span>
                        </p>
                      </div>

                      <div className="flex items-center gap-1.5 flex-shrink-0">
                        <button
                          type="button"
                          onClick={() => updateQuantity(item.productId, -1)}
                          className="w-6 h-6 rounded bg-slate-100 hover:bg-slate-200 flex items-center justify-center text-slate-700 transition"
                        >
                          <Minus className="w-3 h-3" />
                        </button>
                        <span className="w-6 text-center font-bold text-slate-900">{item.quantity}</span>
                        <button
                          type="button"
                          onClick={() => updateQuantity(item.productId, 1)}
                          className="w-6 h-6 rounded bg-slate-100 hover:bg-slate-200 flex items-center justify-center text-slate-700 transition"
                        >
                          <Plus className="w-3 h-3" />
                        </button>
                        <button
                          type="button"
                          onClick={() => removeFromCart(item.productId)}
                          className="p-1 text-slate-400 hover:text-rose-600 transition ml-1"
                        >
                          <Trash2 className="w-3.5 h-3.5" />
                        </button>
                      </div>
                    </div>
                  ))}
                </div>
              )}

              {/* Customer and Payment Details */}
              <div className="space-y-3 pt-3 border-t border-slate-100">
                <div className="grid grid-cols-2 gap-2">
                  <div>
                    <label className="block text-[11px] font-semibold text-slate-600 uppercase mb-1">
                      Customer Name
                    </label>
                    <input
                      type="text"
                      value={customerName}
                      onChange={(e) => setCustomerName(e.target.value)}
                      placeholder="Walk-in Customer"
                      className="w-full px-2.5 py-1.5 text-xs bg-white border border-slate-200 rounded-lg focus:outline-none focus:ring-1 focus:ring-indigo-500"
                    />
                  </div>
                  <div>
                    <label className="block text-[11px] font-semibold text-slate-600 uppercase mb-1">
                      Phone Number
                    </label>
                    <input
                      type="text"
                      value={customerPhone}
                      onChange={(e) => setCustomerPhone(e.target.value)}
                      placeholder="+91 9876543210"
                      className="w-full px-2.5 py-1.5 text-xs bg-white border border-slate-200 rounded-lg focus:outline-none focus:ring-1 focus:ring-indigo-500"
                    />
                  </div>
                </div>

                <div>
                  <label className="block text-[11px] font-semibold text-slate-600 uppercase mb-1">
                    Payment Method *
                  </label>
                  <div className="grid grid-cols-4 gap-1.5 text-xs">
                    {['UPI', 'Cash', 'Card', 'Credit / Udhaar'].map((m) => (
                      <button
                        key={m}
                        type="button"
                        onClick={() => setPaymentMethod(m)}
                        className={`py-1.5 px-2 rounded-lg border font-semibold text-center transition ${
                          paymentMethod === m
                            ? 'bg-indigo-600 text-white border-indigo-600'
                            : 'bg-white border-slate-200 text-slate-700 hover:bg-slate-50'
                        }`}
                      >
                        {m}
                      </button>
                    ))}
                  </div>
                </div>

                <div>
                  <label className="block text-[11px] font-semibold text-slate-600 uppercase mb-1">
                    Bill Notes (Optional)
                  </label>
                  <input
                    type="text"
                    value={notes}
                    onChange={(e) => setNotes(e.target.value)}
                    placeholder="e.g. Cash counter checkout"
                    className="w-full px-2.5 py-1.5 text-xs bg-white border border-slate-200 rounded-lg focus:outline-none focus:ring-1 focus:ring-indigo-500"
                  />
                </div>
              </div>
            </div>

            {/* Bill Summary & Complete Sale Button */}
            <div className="mt-5 pt-4 border-t border-slate-200 space-y-3">
              <div className="flex justify-between items-center text-sm">
                <span className="text-slate-500">Subtotal:</span>
                <span className="font-semibold text-slate-900">{formatCurrency(subtotal)}</span>
              </div>
              <div className="flex justify-between items-baseline text-base">
                <span className="font-bold text-slate-900">Total Payable:</span>
                <span className="text-2xl font-black text-indigo-700">{formatCurrency(subtotal)}</span>
              </div>

              <button
                type="submit"
                disabled={saleMutation.isPending || cartItems.length === 0}
                className="w-full py-3 bg-emerald-600 hover:bg-emerald-700 text-white text-sm font-bold rounded-xl shadow-md transition flex items-center justify-center gap-2 disabled:opacity-50"
              >
                {saleMutation.isPending ? (
                  <>
                    <Loader2 className="w-5 h-5 animate-spin" /> Finalizing Bill...
                  </>
                ) : (
                  <>
                    <CheckCircle2 className="w-5 h-5" /> Complete Sale ({formatCurrency(subtotal)})
                  </>
                )}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
}

export default NewSalePage;
