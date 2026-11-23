import React, { useState, useEffect } from 'react';
import { useNavigate, useLocation, Link } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  Truck,
  Plus,
  Trash2,
  ArrowLeft,
  CheckCircle2,
  Loader2,
  Sparkles,
} from 'lucide-react';
import productService from '../../services/productService';
import supplierService from '../../services/supplierService';
import purchaseService from '../../services/purchaseService';
import PageHeader from '../../components/common/PageHeader';
import ScanBillModal from '../../components/purchases/ScanBillModal';
import { formatCurrency } from '../../utils/formatters';
import { toast } from 'sonner';

export function NewPurchasePage() {
  const navigate = useNavigate();
  const location = useLocation();
  const queryClient = useQueryClient();

  const prefillData = location.state?.prefill || null;
  const ocrData = location.state?.prefillFromOcr || null;

  const [supplierId, setSupplierId] = useState('');
  const [invoiceNumber, setInvoiceNumber] = useState(
    `BILL-${new Date().getFullYear()}-${Math.floor(1000 + Math.random() * 9000)}`
  );
  const [purchaseDate, setPurchaseDate] = useState(
    new Date().toISOString().split('T')[0]
  );
  const [notes, setNotes] = useState('');
  const [items, setItems] = useState([]);
  const [isScanOpen, setIsScanOpen] = useState(false);

  const { data: suppliersData } = useQuery({
    queryKey: ['suppliers'],
    queryFn: () => supplierService.getSuppliers(),
  });

  const { data: productsData } = useQuery({
    queryKey: ['products'],
    queryFn: () => productService.getProducts({ pageSize: 100 }),
  });

  const suppliers = suppliersData?.items || (Array.isArray(suppliersData) ? suppliersData : []);
  const products = productsData?.items || (Array.isArray(productsData) ? productsData : []);

  useEffect(() => {
    if (suppliers.length > 0 && !supplierId) {
      setSupplierId(String(suppliers[0].id));
    }
  }, [suppliers, supplierId]);

  // Handle OCR extraction or AI recommendations
  useEffect(() => {
    if (ocrData) {
      if (ocrData.invoiceNumber) setInvoiceNumber(ocrData.invoiceNumber);
      if (ocrData.notes) setNotes(ocrData.notes);
      if (ocrData.supplierName && suppliers.length > 0) {
        const ocrSupName = ocrData.supplierName.toLowerCase();
        const foundSup = suppliers.find((s) =>
          (s.supplierName || s.name || '').toLowerCase().includes(ocrSupName)
        );
        if (foundSup) setSupplierId(String(foundSup.id));
      }

      if (ocrData.items && Array.isArray(ocrData.items)) {
        const ocrItems = ocrData.items.map((oi) => {
          const oiName = (oi.productName || '').toLowerCase();
          const matched = products.find((p) =>
            (p.name || '').toLowerCase().includes(oiName)
          );
          return {
            productId: matched ? matched.id : (products[0]?.id || 'prod-1'),
            productName: matched ? matched.name : oi.productName,
            sku: matched ? matched.sku : 'SKU-OCR',
            quantity: Number(oi.quantity) || 10,
            unitCost: Number(oi.unitPrice ?? oi.unitCost) || 50,
            unit: matched ? (matched.unitName || matched.unit) : 'Piece',
          };
        });
        setItems(ocrItems);
        toast.success(`Pre-filled ${ocrItems.length} items from scanned vendor bill!`);
      }
    } else if (prefillData && Array.isArray(prefillData) && prefillData.length > 0) {
      const initialItems = prefillData.map((item) => ({
        productId: item.productId,
        productName: item.productName,
        sku: item.sku,
        quantity: item.suggestedOrderQuantity || item.suggestedQuantity || 10,
        unitCost: item.unitPurchasePrice || item.unitCost || 0,
        unit: item.unitName || item.unit || 'Piece',
      }));
      setItems(initialItems);
      if (prefillData[0]?.preferredSupplierId) {
        setSupplierId(String(prefillData[0].preferredSupplierId));
      }
      toast.info(`Pre-filled ${initialItems.length} AI-recommended restock items!`);
    } else if (items.length === 0 && products.length > 0) {
      const first = products[0];
      setItems([
        {
          productId: first.id,
          productName: first.name,
          sku: first.sku,
          quantity: 10,
          unitCost: first.purchasePrice,
          unit: first.unitName || 'Piece',
        },
      ]);
    }
  }, [ocrData, prefillData, products, suppliers]);

  const purchaseMutation = useMutation({
    mutationFn: (data) => purchaseService.createPurchase(data),
    onSuccess: (newPurchase) => {
      queryClient.invalidateQueries({ queryKey: ['purchases'] });
      queryClient.invalidateQueries({ queryKey: ['products'] });
      queryClient.invalidateQueries({ queryKey: ['inventory'] });
      queryClient.invalidateQueries({ queryKey: ['dashboard'] });
      queryClient.invalidateQueries({ queryKey: ['notifications'] });
      toast.success(`Purchase recorded! Bill #${newPurchase.invoiceNumber}`);
      navigate(`/purchases/${newPurchase.id}`);
    },
    onError: (err) => {
      toast.error(err.message || 'Failed to record purchase');
    },
  });

  const handleProductSelect = (index, prodId) => {
    const selected = products.find((p) => String(p.id) === String(prodId));
    if (!selected) return;

    const updated = [...items];
    updated[index] = {
      ...updated[index],
      productId: selected.id,
      productName: selected.name,
      sku: selected.sku,
      unitCost: selected.purchasePrice,
      unit: selected.unitName || selected.unit,
    };
    setItems(updated);
  };

  const handleItemChange = (index, field, value) => {
    const updated = [...items];
    updated[index] = {
      ...updated[index],
      [field]: Number(value) >= 0 ? Number(value) : 0,
    };
    setItems(updated);
  };

  const addItemRow = () => {
    const defaultProduct = products[0] || { id: 'prod-1', name: 'General', sku: 'SKU-001', purchasePrice: 100 };
    setItems([
      ...items,
      {
        productId: defaultProduct.id,
        productName: defaultProduct.name,
        sku: defaultProduct.sku,
        quantity: 10,
        unitCost: defaultProduct.purchasePrice,
        unit: defaultProduct.unitName || 'Piece',
      },
    ]);
  };

  const removeItemRow = (index) => {
    if (items.length <= 1) {
      toast.error('At least one item is required in the purchase');
      return;
    }
    setItems(items.filter((_, i) => i !== index));
  };

  const estimatedTotal = items.reduce((sum, item) => sum + (item.quantity * item.unitCost), 0);

  const handleSubmit = (e) => {
    e.preventDefault();
    if (!supplierId) {
      toast.error('Please select a supplier');
      return;
    }
    if (!invoiceNumber.trim()) {
      toast.error('Please enter the supplier bill / invoice number');
      return;
    }
    if (items.length === 0) {
      toast.error('Please add at least one product');
      return;
    }

    // Call POST /api/v1/purchases
    purchaseMutation.mutate({
      supplierId,
      invoiceNumber: invoiceNumber.trim(),
      purchaseDate: new Date(purchaseDate).toISOString(),
      notes: notes.trim(),
      items: items.map((i) => ({
        productId: i.productId,
        quantity: Number(i.quantity),
        unitCost: Number(i.unitCost),
      })),
    });
  };

  return (
    <div className="max-w-4xl mx-auto space-y-6">
      <PageHeader
        title="Record Wholesale Purchase"
        subtitle="Log received vendor bills to automatically increment shelf inventory."
        breadcrumbs={[
          { label: 'Purchases', to: '/purchases' },
          { label: 'New Purchase' },
        ]}
        action={
          <div className="flex items-center gap-2">
            <button
              type="button"
              onClick={() => setIsScanOpen(true)}
              className="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-bold text-indigo-700 bg-indigo-50 hover:bg-indigo-100 border border-indigo-200 rounded-lg transition"
            >
              <Sparkles className="w-3.5 h-3.5 text-amber-500" />
              <span>Scan Bill (AI OCR)</span>
            </button>
            <Link
              to="/purchases"
              className="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold text-slate-700 bg-white border border-slate-200 rounded-lg hover:bg-slate-50 transition"
            >
              <ArrowLeft className="w-3.5 h-3.5" /> Back
            </Link>
          </div>
        }
      />

      <form onSubmit={handleSubmit} className="space-y-6">
        {/* Bill metadata */}
        <div className="bg-white rounded-xl border border-slate-200 p-5 shadow-xs grid grid-cols-1 sm:grid-cols-3 gap-4">
          <div>
            <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1">
              Select Supplier *
            </label>
            <select
              value={supplierId}
              onChange={(e) => setSupplierId(e.target.value)}
              className="w-full px-3 py-2 text-sm bg-white border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-600"
              required
            >
              <option value="">Select supplier...</option>
              {suppliers.map((s) => (
                <option key={s.id} value={s.id}>
                  {s.supplierName || s.name} ({s.contactPerson})
                </option>
              ))}
            </select>
          </div>

          <div>
            <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1">
              Supplier Bill / Invoice # *
            </label>
            <input
              type="text"
              value={invoiceNumber}
              onChange={(e) => setInvoiceNumber(e.target.value)}
              placeholder="e.g. BILL-LK-9082"
              className="w-full px-3 py-2 text-sm bg-white border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-600 font-mono"
              required
            />
          </div>

          <div>
            <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1">
              Purchase Date *
            </label>
            <input
              type="date"
              value={purchaseDate}
              onChange={(e) => setPurchaseDate(e.target.value)}
              className="w-full px-3 py-2 text-sm bg-white border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-600"
              required
            />
          </div>

          <div className="sm:col-span-3">
            <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1">
              Purchase Notes (Optional)
            </label>
            <input
              type="text"
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              placeholder="e.g. Weekly milk & dairy replenishment"
              className="w-full px-3 py-2 text-sm bg-white border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-600"
            />
          </div>
        </div>

        {/* Itemized lines */}
        <div className="bg-white rounded-xl border border-slate-200 p-5 shadow-xs">
          <div className="flex items-center justify-between pb-3 border-b border-slate-100 mb-4">
            <div>
              <h3 className="text-base font-bold text-slate-900">Delivered Line Items</h3>
              <p className="text-xs text-slate-500">Add SKUs delivered, cost per unit, and received quantities.</p>
            </div>
            <button
              type="button"
              onClick={addItemRow}
              className="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold text-indigo-700 bg-indigo-50 hover:bg-indigo-100 rounded-lg transition"
            >
              <Plus className="w-3.5 h-3.5" />
              <span>Add Line Item</span>
            </button>
          </div>

          <div className="space-y-3">
            {items.map((item, idx) => (
              <div
                key={idx}
                className="p-3 rounded-lg border border-slate-200 bg-slate-50/50 grid grid-cols-1 sm:grid-cols-12 gap-3 items-center text-xs"
              >
                <div className="sm:col-span-5">
                  <label className="block text-[10px] font-semibold text-slate-500 uppercase mb-1">
                    Product Item
                  </label>
                  <select
                    value={item.productId}
                    onChange={(e) => handleProductSelect(idx, e.target.value)}
                    className="w-full px-2.5 py-1.5 text-xs bg-white border border-slate-200 rounded-lg focus:outline-none focus:ring-1 focus:ring-indigo-500"
                  >
                    {products.map((p) => (
                      <option key={p.id} value={p.id}>
                        {p.name} ({p.sku})
                      </option>
                    ))}
                  </select>
                </div>

                <div className="sm:col-span-2">
                  <label className="block text-[10px] font-semibold text-slate-500 uppercase mb-1">
                    Received Qty
                  </label>
                  <input
                    type="number"
                    min="1"
                    value={item.quantity}
                    onChange={(e) => handleItemChange(idx, 'quantity', e.target.value)}
                    className="w-full px-2.5 py-1.5 text-xs bg-white border border-slate-200 rounded-lg focus:outline-none focus:ring-1 focus:ring-indigo-500 font-semibold"
                  />
                </div>

                <div className="sm:col-span-2">
                  <label className="block text-[10px] font-semibold text-slate-500 uppercase mb-1">
                    Unit Cost (₹)
                  </label>
                  <input
                    type="number"
                    step="any"
                    value={item.unitCost}
                    onChange={(e) => handleItemChange(idx, 'unitCost', e.target.value)}
                    className="w-full px-2.5 py-1.5 text-xs bg-white border border-slate-200 rounded-lg focus:outline-none focus:ring-1 focus:ring-indigo-500"
                  />
                </div>

                <div className="sm:col-span-2 text-right">
                  <span className="block text-[10px] font-semibold text-slate-500 uppercase mb-1">
                    Line Total
                  </span>
                  <p className="font-bold text-slate-900 text-sm">
                    {formatCurrency(item.quantity * item.unitCost)}
                  </p>
                </div>

                <div className="sm:col-span-1 text-center">
                  <button
                    type="button"
                    onClick={() => removeItemRow(idx)}
                    className="p-1.5 text-slate-400 hover:text-rose-600 transition"
                    title="Remove item"
                  >
                    <Trash2 className="w-4 h-4" />
                  </button>
                </div>
              </div>
            ))}
          </div>

          {/* Totals & Submit */}
          <div className="mt-6 pt-4 border-t border-slate-200 flex flex-col sm:flex-row items-center justify-between gap-4">
            <div className="text-left">
              <span className="text-xs text-slate-500">Total Purchase Cost:</span>
              <p className="text-2xl font-bold text-slate-900">{formatCurrency(estimatedTotal)}</p>
            </div>

            <div className="flex items-center gap-3">
              <button
                type="button"
                onClick={() => navigate('/purchases')}
                className="px-4 py-2 text-sm font-medium text-slate-700 bg-white border border-slate-200 rounded-lg hover:bg-slate-50"
              >
                Cancel
              </button>
              <button
                type="submit"
                disabled={purchaseMutation.isPending || items.length === 0}
                className="px-6 py-2 bg-blue-600 hover:bg-blue-700 text-white text-sm font-semibold rounded-lg shadow-sm transition flex items-center gap-2 disabled:opacity-50"
              >
                {purchaseMutation.isPending ? (
                  <>
                    <Loader2 className="w-4 h-4 animate-spin" /> Saving...
                  </>
                ) : (
                  <>
                    <CheckCircle2 className="w-4 h-4" /> Confirm & Inward Stock
                  </>
                )}
              </button>
            </div>
          </div>
        </div>
      </form>

      {/* OCR Scanner */}
      <ScanBillModal
        isOpen={isScanOpen}
        onClose={() => setIsScanOpen(false)}
        onExtracted={(extracted) => {
          if (extracted.invoiceNumber) setInvoiceNumber(extracted.invoiceNumber);
          if (extracted.notes) setNotes(extracted.notes);
          if (extracted.items && Array.isArray(extracted.items)) {
            const mapped = extracted.items.map((oi) => {
              const oiName = (oi.productName || '').toLowerCase();
              const matched = products.find((p) =>
                (p.name || '').toLowerCase().includes(oiName)
              );
              return {
                productId: matched ? matched.id : (products[0]?.id || 'prod-1'),
                productName: matched ? matched.name : oi.productName,
                sku: matched ? matched.sku : 'SKU-OCR',
                quantity: Number(oi.quantity) || 10,
                unitCost: Number(oi.unitPrice ?? oi.unitCost) || 50,
                unit: matched ? (matched.unitName || matched.unit) : 'Piece',
              };
            });
            setItems(mapped);
          }
        }}
      />
    </div>
  );
}

export default NewPurchasePage;
