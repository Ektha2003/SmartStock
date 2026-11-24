import React, { useState, useMemo } from 'react';
import { createPortal } from 'react-dom';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  ArrowLeft,
  FileSpreadsheet,
  CheckCircle2,
  XCircle,
  AlertTriangle,
  RefreshCw,
  Plus,
  TrendingUp,
  TrendingDown,
  Building2,
  Calendar,
  CreditCard,
  FileText,
  Boxes,
  HelpCircle,
  ExternalLink,
  Loader2,
  Check,
  Eye,
  Info
} from 'lucide-react';
import supplierInvoiceService from '../../services/supplierInvoiceService';
import productService from '../../services/productService';
import categoryService from '../../services/categoryService';
import PageHeader from '../../components/common/PageHeader';
import StatusBadge from '../../components/common/StatusBadge';
import InvoiceComparisonModal from './InvoiceComparisonModal';
import { formatCurrency, formatDate } from '../../utils/formatters';
import { toast } from 'sonner';

export function InvoiceReviewPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const [isCompareOpen, setIsCompareOpen] = useState(false);
  const [newProductModalItem, setNewProductModalItem] = useState(null);
  const [approvalNotes, setApprovalNotes] = useState('');
  const [updatePrices, setUpdatePrices] = useState(true);

  // New product form state
  const [newProductName, setNewProductName] = useState('');
  const [newProductSku, setNewProductSku] = useState('');
  const [newProductCategoryId, setNewProductCategoryId] = useState('');
  const [newProductUnit, setNewProductUnit] = useState('Piece');
  const [newProductPurchasePrice, setNewProductPurchasePrice] = useState(0);
  const [newProductSellingPrice, setNewProductSellingPrice] = useState(0);
  const [newProductConversion, setNewProductConversion] = useState(1);
  const [newProductPackSize, setNewProductPackSize] = useState('');

  // Fetch invoice
  const { data: invoice, isLoading, error, refetch } = useQuery({
    queryKey: ['supplier-invoice', id],
    queryFn: () => supplierInvoiceService.getInvoiceById(id),
    enabled: !!id,
  });

  // Fetch catalog products for dropdown selection
  const { data: productsData } = useQuery({
    queryKey: ['all-products-for-match'],
    queryFn: () => productService.getProducts({ pageSize: 200 }),
  });

  const catalogProducts = useMemo(() => {
    if (Array.isArray(productsData)) return productsData;
    if (productsData?.items && Array.isArray(productsData.items)) return productsData.items;
    return [];
  }, [productsData]);

  // Fetch categories for product creation
  const { data: categoriesData } = useQuery({
    queryKey: ['categories'],
    queryFn: () => categoryService.getCategories(),
  });

  const categories = useMemo(() => {
    if (Array.isArray(categoriesData)) return categoriesData;
    if (categoriesData?.items && Array.isArray(categoriesData.items)) return categoriesData.items;
    return [];
  }, [categoriesData]);

  // Mutations
  const rematchMutation = useMutation({
    mutationFn: () => supplierInvoiceService.rematchProducts(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['supplier-invoice', id] });
      toast.success('Line items rematched against inventory catalog.');
    },
    onError: (err) => toast.error(err.message || 'Failed to rematch items'),
  });

  const updateItemMutation = useMutation({
    mutationFn: ({ itemId, data }) => supplierInvoiceService.updateItem(id, itemId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['supplier-invoice', id] });
      toast.success('Item details updated.');
    },
    onError: (err) => toast.error(err.message || 'Failed to update item'),
  });

  const createProductMutation = useMutation({
    mutationFn: (data) => supplierInvoiceService.createProductFromInvoice(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['supplier-invoice', id] });
      queryClient.invalidateQueries({ queryKey: ['all-products-for-match'] });
      toast.success('New product created and linked to invoice item!');
      setNewProductModalItem(null);
    },
    onError: (err) => toast.error(err.message || 'Failed to create product'),
  });

  const approveMutation = useMutation({
    mutationFn: () => supplierInvoiceService.approveInvoice(id, { notes: approvalNotes, updatePurchasePrices: updatePrices }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['supplier-invoice', id] });
      queryClient.invalidateQueries({ queryKey: ['supplier-invoices'] });
      queryClient.invalidateQueries({ queryKey: ['products'] });
      toast.success('Invoice approved! Stock quantities and purchase orders updated successfully.');
      navigate('/supplier-invoices');
    },
    onError: (err) => toast.error(err.message || 'Failed to approve invoice'),
  });

  const rejectMutation = useMutation({
    mutationFn: (reason) => supplierInvoiceService.rejectInvoice(id, reason),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['supplier-invoice', id] });
      queryClient.invalidateQueries({ queryKey: ['supplier-invoices'] });
      toast.success('Invoice rejected.');
      navigate('/supplier-invoices');
    },
    onError: (err) => toast.error(err.message || 'Failed to reject invoice'),
  });

  const handleOpenAddProduct = (item) => {
    setNewProductModalItem(item);
    setNewProductName(item.rawProductName);
    setNewProductSku(item.rawSupplierSku || `SKU-${Date.now().toString().slice(-6)}`);
    setNewProductCategoryId(categories[0]?.id || '');
    setNewProductUnit(item.invoiceUnit === 'KG' ? 'Kg' : item.invoiceUnit === 'LITRE' ? 'Litre' : 'Piece');
    setNewProductPurchasePrice(item.invoiceUnitPrice);
    setNewProductSellingPrice(Math.round(item.invoiceUnitPrice * 1.25 * 100) / 100);
    setNewProductConversion(item.conversionFactor || 1);
    setNewProductPackSize(item.packSize || '');
  };

  const handleSaveNewProduct = (e) => {
    e.preventDefault();
    if (!newProductModalItem || !newProductCategoryId) {
      toast.error('Please select a category for the product.');
      return;
    }

    createProductMutation.mutate({
      invoiceItemId: newProductModalItem.id,
      name: newProductName,
      sku: newProductSku,
      categoryId: newProductCategoryId,
      unit: newProductUnit,
      purchasePrice: parseFloat(newProductPurchasePrice) || 0,
      sellingPrice: parseFloat(newProductSellingPrice) || 0,
      conversionFactor: parseFloat(newProductConversion) || 1,
      packSize: newProductPackSize ? parseFloat(newProductPackSize) : null,
    });
  };

  const handleProductSelect = (itemId, productId) => {
    if (!productId) return;
    updateItemMutation.mutate({
      itemId,
      data: {
        matchedProductId: productId,
      },
    });
  };

  const handleConversionChange = (item, conversionFactor) => {
    const val = parseFloat(conversionFactor);
    if (isNaN(val) || val <= 0) return;
    updateItemMutation.mutate({
      itemId: item.id,
      data: {
        conversionFactor: val,
        calculatedStockQuantity: item.invoiceQuantity * val,
      },
    });
  };

  const handleTogglePriceUpdate = (item, checked) => {
    updateItemMutation.mutate({
      itemId: item.id,
      data: {
        shouldUpdatePurchasePrice: checked,
      },
    });
  };

  const handleToggleIgnored = (item, checked) => {
    updateItemMutation.mutate({
      itemId: item.id,
      data: {
        isIgnored: checked,
      },
    });
  };

  if (isLoading) {
    return (
      <div className="flex flex-col items-center justify-center min-h-[400px] gap-3">
        <Loader2 className="w-9 h-9 animate-spin text-indigo-600" />
        <p className="text-sm text-slate-500 font-medium">Loading extracted invoice review...</p>
      </div>
    );
  }

  if (error || !invoice) {
    return (
      <div className="bg-white p-8 rounded-xl border border-slate-200 text-center max-w-md mx-auto my-12">
        <AlertTriangle className="w-10 h-10 text-rose-500 mx-auto mb-3" />
        <h3 className="text-base font-semibold text-slate-800">Invoice Not Found</h3>
        <p className="text-sm text-slate-500 mt-1 mb-5">The requested invoice could not be retrieved.</p>
        <button
          onClick={() => navigate('/supplier-invoices')}
          className="inline-flex items-center gap-2 px-4 py-2 bg-indigo-600 text-white text-sm font-medium rounded-lg"
        >
          <ArrowLeft className="w-4 h-4" />
          Back to Invoices
        </button>
      </div>
    );
  }

  const items = invoice.items || [];
  const activeItems = items.filter((i) => !i.isIgnored);
  const unmatchedActive = activeItems.filter((i) => !i.matchedProductId);
  const priceHikeItems = items.filter((i) => i.priceDifference > 0);
  const isApproved = invoice.status === 'Approved';
  const isDuplicate = invoice.isDuplicate || invoice.status === 'Duplicate';

  return (
    <div className="space-y-6 pb-20">
      <PageHeader
        title={`Review Invoice #${invoice.invoiceNumber}`}
        subtitle={`Uploaded ${formatDate(invoice.createdAt)} • ${invoice.originalFileName}`}
        breadcrumbs={[
          { label: 'Supplier Invoices', href: '/supplier-invoices' },
          { label: `Invoice #${invoice.invoiceNumber}` },
        ]}
        action={
          <div className="flex items-center gap-2">
            <button
              onClick={() => setIsCompareOpen(true)}
              className="inline-flex items-center gap-2 px-3.5 py-2 bg-indigo-50 text-indigo-700 hover:bg-indigo-100 text-sm font-semibold rounded-lg transition"
            >
              <TrendingUp className="w-4 h-4" />
              <span>Compare with Previous</span>
            </button>
            {!isApproved && (
              <button
                onClick={() => rematchMutation.mutate()}
                disabled={rematchMutation.isPending}
                className="inline-flex items-center gap-2 px-3.5 py-2 bg-white border border-slate-200 text-slate-700 hover:bg-slate-50 text-sm font-semibold rounded-lg shadow-xs transition"
              >
                <RefreshCw className={`w-4 h-4 ${rematchMutation.isPending ? 'animate-spin' : ''}`} />
                <span>Rematch Items</span>
              </button>
            )}
            <button
              onClick={() => navigate('/supplier-invoices')}
              className="inline-flex items-center gap-2 px-3.5 py-2 bg-white border border-slate-200 text-slate-700 hover:bg-slate-50 text-sm font-semibold rounded-lg shadow-xs transition"
            >
              <ArrowLeft className="w-4 h-4" />
              <span>Back</span>
            </button>
          </div>
        }
      />

      {/* Duplicate Warning Alert */}
      {isDuplicate && (
        <div className="p-4 bg-rose-50 border border-rose-200 rounded-xl flex items-start gap-3 text-rose-900">
          <AlertTriangle className="w-5 h-5 text-rose-600 shrink-0 mt-0.5" />
          <div className="text-sm">
            <p className="font-bold">Potential Duplicate Invoice Detected</p>
            <p className="mt-0.5 text-rose-700">
              {invoice.duplicateReason || 'This document has identical contents or matches a previously submitted invoice number.'}
            </p>
          </div>
        </div>
      )}

      {/* Invoice Overview Grid */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-5">
        {/* Vendor & Header Info */}
        <div className="bg-white p-5 rounded-xl border border-slate-200 shadow-xs space-y-3">
          <div className="flex items-center justify-between border-b border-slate-100 pb-3">
            <h3 className="text-sm font-semibold text-slate-900 flex items-center gap-2">
              <Building2 className="w-4 h-4 text-indigo-600" />
              Supplier & Invoice Details
            </h3>
            <StatusBadge status={invoice.status} size="sm" />
          </div>

          <div className="space-y-2.5 text-sm">
            <div>
              <p className="text-xs text-slate-400 font-medium">Supplier Name</p>
              <p className="text-slate-800 font-bold">{invoice.supplierName || invoice.supplierNameRaw || 'Unassigned'}</p>
            </div>
            <div className="grid grid-cols-2 gap-2">
              <div>
                <p className="text-xs text-slate-400 font-medium">Invoice Date</p>
                <p className="text-slate-800 font-medium">{formatDate(invoice.invoiceDate)}</p>
              </div>
              <div>
                <p className="text-xs text-slate-400 font-medium">Due Date</p>
                <p className="text-slate-800 font-medium">{invoice.dueDate ? formatDate(invoice.dueDate) : 'Immediate'}</p>
              </div>
            </div>
            <div className="grid grid-cols-2 gap-2">
              <div>
                <p className="text-xs text-slate-400 font-medium">GSTIN</p>
                <p className="text-slate-800 font-mono text-xs font-semibold">{invoice.supplierGst || 'Not present'}</p>
              </div>
              <div>
                <p className="text-xs text-slate-400 font-medium">Payment Terms</p>
                <p className="text-slate-800 font-medium text-xs">{invoice.paymentTerms || 'Standard'}</p>
              </div>
            </div>
            {invoice.filePath && (
              <div className="pt-2">
                <a
                  href={invoice.filePath}
                  target="_blank"
                  rel="noreferrer"
                  className="inline-flex items-center gap-1.5 text-xs text-indigo-600 hover:text-indigo-800 font-semibold"
                >
                  <Eye className="w-3.5 h-3.5" />
                  <span>View Original Uploaded Document</span>
                </a>
              </div>
            )}
          </div>
        </div>

        {/* Tax & Financial Breakdown */}
        <div className="bg-white p-5 rounded-xl border border-slate-200 shadow-xs space-y-3">
          <h3 className="text-sm font-semibold text-slate-900 flex items-center gap-2 border-b border-slate-100 pb-3">
            <CreditCard className="w-4 h-4 text-indigo-600" />
            Amount & Tax Breakdown
          </h3>

          <div className="space-y-2 text-sm">
            <div className="flex justify-between text-slate-600">
              <span>Subtotal:</span>
              <span className="font-mono">{formatCurrency(invoice.subTotal)}</span>
            </div>
            {invoice.discount > 0 && (
              <div className="flex justify-between text-emerald-600">
                <span>Discount:</span>
                <span className="font-mono">-{formatCurrency(invoice.discount)}</span>
              </div>
            )}
            <div className="flex justify-between text-slate-500 text-xs">
              <span>Taxable Amount:</span>
              <span className="font-mono">{formatCurrency(invoice.taxableAmount)}</span>
            </div>
            <div className="flex justify-between text-slate-500 text-xs">
              <span>CGST + SGST:</span>
              <span className="font-mono">{formatCurrency(invoice.cgst + invoice.sgst)}</span>
            </div>
            {invoice.igst > 0 && (
              <div className="flex justify-between text-slate-500 text-xs">
                <span>IGST:</span>
                <span className="font-mono">{formatCurrency(invoice.igst)}</span>
              </div>
            )}
            <div className="border-t border-slate-200 pt-2 flex justify-between items-center text-slate-900 font-bold">
              <span>Grand Total:</span>
              <span className="text-lg text-indigo-600 font-mono">{formatCurrency(invoice.grandTotal)}</span>
            </div>
          </div>
        </div>

        {/* Match & Review Status Card */}
        <div className="bg-white p-5 rounded-xl border border-slate-200 shadow-xs space-y-4 flex flex-col justify-between">
          <div>
            <h3 className="text-sm font-semibold text-slate-900 flex items-center gap-2 border-b border-slate-100 pb-3">
              <Boxes className="w-4 h-4 text-indigo-600" />
              Inventory Sync Status
            </h3>

            <div className="grid grid-cols-3 gap-2 text-center mt-3">
              <div className="bg-slate-50 p-2.5 rounded-lg border border-slate-100">
                <p className="text-[11px] text-slate-500 font-medium">Total Items</p>
                <p className="text-base font-bold text-slate-900 mt-0.5">{items.length}</p>
              </div>
              <div className="bg-emerald-50 p-2.5 rounded-lg border border-emerald-100">
                <p className="text-[11px] text-emerald-700 font-medium">Matched</p>
                <p className="text-base font-bold text-emerald-800 mt-0.5">{invoice.matchedItemsCount}</p>
              </div>
              <div className={`p-2.5 rounded-lg border ${
                invoice.unmatchedItemsCount > 0
                  ? 'bg-amber-50 border-amber-200 text-amber-900'
                  : 'bg-slate-50 border-slate-100 text-slate-700'
              }`}>
                <p className="text-[11px] font-medium">Unmatched</p>
                <p className="text-base font-bold mt-0.5">{invoice.unmatchedItemsCount}</p>
              </div>
            </div>

            {priceHikeItems.length > 0 && (
              <div className="mt-3 p-2.5 bg-rose-50 border border-rose-200/80 rounded-lg text-xs text-rose-800 flex items-center gap-2">
                <TrendingUp className="w-4 h-4 text-rose-600 shrink-0" />
                <span>
                  <strong>{priceHikeItems.length} item(s)</strong> have a higher purchase price than existing stock.
                </span>
              </div>
            )}
          </div>

          <div className="text-xs text-slate-500">
            {isApproved ? (
              <p className="text-emerald-700 font-medium flex items-center gap-1.5">
                <CheckCircle2 className="w-4 h-4" />
                Approved by {invoice.approvedByUserName || 'Admin'} on {formatDate(invoice.approvedAt)}
              </p>
            ) : (
              <p className="italic">
                Verify each product mapping, pack conversion, and unit rates before final approval.
              </p>
            )}
          </div>
        </div>
      </div>

      {/* Main Line Items Matching Table */}
      <div className="bg-white rounded-xl border border-slate-200 shadow-xs overflow-hidden">
        <div className="px-6 py-4 border-b border-slate-100 flex items-center justify-between bg-slate-50/70">
          <div>
            <h3 className="font-bold text-slate-900 text-base flex items-center gap-2">
              <Boxes className="w-5 h-5 text-indigo-600" />
              Invoice Line Items & Product Matching
            </h3>
            <p className="text-xs text-slate-500 mt-0.5">
              Confirm product mappings, unit conversion factors (e.g. 20 PCS × 5 KG = 100 KG), and purchase rate updates.
            </p>
          </div>
        </div>

        <div className="overflow-x-auto">
          <table className="w-full text-left border-collapse">
            <thead>
              <tr className="bg-slate-50 border-b border-slate-200 text-xs font-semibold text-slate-500">
                <th className="py-3 px-3">Invoice Product (Raw OCR)</th>
                <th className="py-3 px-3">Invoice Qty & Rate</th>
                <th className="py-3 px-3">Matching Status</th>
                <th className="py-3 px-3">Linked Store Product</th>
                <th className="py-3 px-3">Pack & Conversion</th>
                <th className="py-3 px-3 text-right">Stock to Add</th>
                <th className="py-3 px-3 text-right">Price Variance</th>
                <th className="py-3 px-3 text-center">Update Rate</th>
                <th className="py-3 px-3 text-center">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100 text-sm">
              {items.map((item) => {
                const isMatched = !!item.matchedProductId;
                const isHike = item.priceDifference > 0;
                const isDrop = item.priceDifference < 0;

                return (
                  <tr
                    key={item.id}
                    className={`transition ${
                      item.isIgnored
                        ? 'bg-slate-100/50 opacity-50'
                        : isHike
                        ? 'bg-rose-50/20 hover:bg-rose-50/40'
                        : 'hover:bg-slate-50/80'
                    }`}
                  >
                    {/* Raw Item Details */}
                    <td className="py-3 px-3 max-w-[200px]">
                      <p className="font-semibold text-slate-900 leading-snug">{item.rawProductName}</p>
                      {item.rawDescription && (
                        <p className="text-xs text-slate-500 truncate" title={item.rawDescription}>
                          {item.rawDescription}
                        </p>
                      )}
                      <div className="flex items-center gap-2 text-[11px] text-slate-400 font-mono mt-0.5">
                        {item.rawSupplierSku && <span>SKU: {item.rawSupplierSku}</span>}
                        {item.rawHsnCode && <span>HSN: {item.rawHsnCode}</span>}
                      </div>
                    </td>

                    {/* Invoice Qty & Rate */}
                    <td className="py-3 px-3 whitespace-nowrap">
                      <p className="font-medium text-slate-900">
                        {item.invoiceQuantity} {item.invoiceUnit}
                      </p>
                      <p className="text-xs text-slate-500 font-mono">
                        @ {formatCurrency(item.invoiceUnitPrice)}
                      </p>
                      <p className="text-xs font-bold text-slate-800 font-mono">
                        = {formatCurrency(item.lineTotal)}
                      </p>
                    </td>

                    {/* Match Status Badge */}
                    <td className="py-3 px-3 whitespace-nowrap">
                      {renderMatchBadge(item.matchStatus, item.matchConfidence)}
                      {item.matchExplanation && (
                        <p className="text-[11px] text-slate-500 mt-1 max-w-[150px] truncate" title={item.matchExplanation}>
                          {item.matchExplanation}
                        </p>
                      )}
                    </td>

                    {/* Linked Store Product Dropdown */}
                    <td className="py-3 px-3 min-w-[220px]">
                      {isApproved ? (
                        <span className="font-semibold text-slate-900">
                          {item.matchedProductName || 'Not Matched'}
                        </span>
                      ) : (
                        <div className="space-y-1">
                          <select
                            value={item.matchedProductId || ''}
                            onChange={(e) => handleProductSelect(item.id, e.target.value)}
                            disabled={item.isIgnored}
                            className="w-full text-xs py-1.5 px-2 bg-white border border-slate-200 rounded-lg focus:outline-none focus:ring-1 focus:ring-indigo-500"
                          >
                            <option value="">-- Select Matched Product --</option>
                            {catalogProducts.map((p) => (
                              <option key={p.id} value={p.id}>
                                {p.name} ({p.sku}) - {p.currentStock} {p.unit} in stock
                              </option>
                            ))}
                          </select>
                          {item.matchedProductName && (
                            <p className="text-[11px] text-emerald-700 font-medium flex items-center gap-1">
                              <Check className="w-3 h-3" />
                              Linked: {item.matchedProductName}
                            </p>
                          )}
                        </div>
                      )}
                    </td>

                    {/* Pack Size & Unit Conversion */}
                    <td className="py-3 px-3 whitespace-nowrap">
                      {isApproved ? (
                        <span className="text-xs font-medium text-slate-700">
                          {item.conversionFactor}× ({item.targetUnit})
                        </span>
                      ) : (
                        <div className="flex items-center gap-1">
                          <input
                            type="number"
                            min="0.001"
                            step="any"
                            value={item.conversionFactor}
                            onChange={(e) => handleConversionChange(item, e.target.value)}
                            disabled={item.isIgnored}
                            className="w-16 text-xs py-1 px-1.5 border border-slate-200 rounded text-center focus:ring-1 focus:ring-indigo-500"
                          />
                          <span className="text-xs text-slate-500">×</span>
                          <span className="text-xs font-mono font-semibold text-slate-700">{item.targetUnit}</span>
                        </div>
                      )}
                    </td>

                    {/* Calculated Stock to Add */}
                    <td className="py-3 px-3 text-right whitespace-nowrap">
                      <p className="font-bold text-slate-900 font-mono">
                        +{item.calculatedStockQuantity}
                      </p>
                      <p className="text-[11px] text-slate-400 font-medium">{item.targetUnit}</p>
                    </td>

                    {/* Price Variance Indicator */}
                    <td className="py-3 px-3 text-right whitespace-nowrap">
                      {item.previousPurchasePrice != null ? (
                        <div>
                          <div className="flex items-center justify-end gap-1 font-mono text-xs">
                            <span className="text-slate-400 line-through">
                              {formatCurrency(item.previousPurchasePrice)}
                            </span>
                            <span>➔</span>
                            <span className="font-bold text-slate-900">
                              {formatCurrency(item.newPurchasePrice)}
                            </span>
                          </div>
                          {item.percentagePriceChange !== 0 && (
                            <div className="mt-1 flex items-center justify-end gap-1">
                              <span className={`inline-flex items-center gap-0.5 text-[11px] font-bold px-1.5 py-0.5 rounded ${
                                isHike ? 'bg-rose-100 text-rose-800' : 'bg-emerald-100 text-emerald-800'
                              }`}>
                                {isHike ? <TrendingUp className="w-3 h-3" /> : <TrendingDown className="w-3 h-3" />}
                                {isHike ? `+${item.percentagePriceChange}%` : `${item.percentagePriceChange}%`}
                              </span>
                            </div>
                          )}
                        </div>
                      ) : (
                        <span className="text-xs text-slate-400">New Item</span>
                      )}
                    </td>

                    {/* Update Purchase Price Checkbox */}
                    <td className="py-3 px-3 text-center">
                      <input
                        type="checkbox"
                        checked={item.shouldUpdatePurchasePrice}
                        onChange={(e) => handleTogglePriceUpdate(item, e.target.checked)}
                        disabled={isApproved || item.isIgnored}
                        className="rounded border-slate-300 text-indigo-600 focus:ring-indigo-500 w-4 h-4"
                        title="Update product's purchase price to this invoice price"
                      />
                    </td>

                    {/* Action buttons (New Product / Ignore) */}
                    <td className="py-3 px-3 text-center whitespace-nowrap">
                      {!isApproved && (
                        <div className="flex items-center justify-center gap-1">
                          {!isMatched && (
                            <button
                              onClick={() => handleOpenAddProduct(item)}
                              className="px-2 py-1 bg-indigo-50 hover:bg-indigo-100 text-indigo-700 text-xs font-semibold rounded transition"
                              title="Create new inventory product from this line item"
                            >
                              + New Product
                            </button>
                          )}
                          <button
                            onClick={() => handleToggleIgnored(item, !item.isIgnored)}
                            className={`px-2 py-1 text-xs font-medium rounded transition ${
                              item.isIgnored
                                ? 'bg-slate-200 text-slate-700 hover:bg-slate-300'
                                : 'text-slate-400 hover:text-slate-600 hover:bg-slate-100'
                            }`}
                            title={item.isIgnored ? 'Include item' : 'Ignore non-inventory item'}
                          >
                            {item.isIgnored ? 'Ignored' : 'Ignore'}
                          </button>
                        </div>
                      )}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      </div>

      {/* Floating Approval Bottom Bar */}
      {!isApproved && (
        <div className="fixed bottom-0 left-0 right-0 z-40 bg-white border-t border-slate-200 px-6 py-3 shadow-lg flex flex-col sm:flex-row sm:items-center justify-between gap-4">
          <div className="flex items-center gap-4 text-sm text-slate-600">
            <div>
              <span className="font-semibold text-slate-900">{activeItems.length} Active Items</span>
              {unmatchedActive.length > 0 && (
                <span className="text-rose-600 font-bold ml-2">({unmatchedActive.length} unmatched)</span>
              )}
            </div>
            <div className="hidden md:flex items-center gap-2">
              <label className="text-xs font-medium text-slate-500">Approval Notes:</label>
              <input
                type="text"
                value={approvalNotes}
                onChange={(e) => setApprovalNotes(e.target.value)}
                placeholder="Optional purchase order notes..."
                className="px-2.5 py-1 text-xs border border-slate-200 rounded-lg w-64 bg-slate-50 focus:bg-white"
              />
            </div>
            <div className="hidden lg:flex items-center gap-1.5 text-xs text-slate-500">
              <input
                type="checkbox"
                id="updatePricesCheck"
                checked={updatePrices}
                onChange={(e) => setUpdatePrices(e.target.checked)}
                className="rounded border-slate-300 text-indigo-600 focus:ring-indigo-500"
              />
              <label htmlFor="updatePricesCheck">Update purchase prices in catalog</label>
            </div>
          </div>

          <div className="flex items-center gap-2 justify-end">
            <button
              onClick={() => {
                const reason = window.prompt('Enter reason for rejection:');
                if (reason) rejectMutation.mutate(reason);
              }}
              disabled={rejectMutation.isPending}
              className="px-4 py-2 border border-rose-200 text-rose-700 hover:bg-rose-50 text-sm font-semibold rounded-lg transition"
            >
              Reject Invoice
            </button>

            <button
              onClick={() => approveMutation.mutate()}
              disabled={approveMutation.isPending || unmatchedActive.length > 0}
              className="px-5 py-2 bg-indigo-600 hover:bg-indigo-700 disabled:opacity-50 disabled:cursor-not-allowed text-white text-sm font-bold rounded-lg shadow-sm flex items-center gap-2 transition"
            >
              {approveMutation.isPending ? (
                <Loader2 className="w-4 h-4 animate-spin" />
              ) : (
                <CheckCircle2 className="w-4 h-4" />
              )}
              <span>Approve & Sync Inventory</span>
            </button>
          </div>
        </div>
      )}

      {/* Add New Product From Invoice Modal */}
      {newProductModalItem && createPortal(
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/40 backdrop-blur-xs">
          <div className="bg-white rounded-2xl border border-slate-200 shadow-xl w-full max-w-lg overflow-hidden flex flex-col">
            <div className="px-6 py-4 border-b border-slate-100 flex items-center justify-between bg-slate-50">
              <h3 className="font-bold text-slate-900 text-base">
                Create Inventory Product from Invoice
              </h3>
              <button
                onClick={() => setNewProductModalItem(null)}
                className="p-1 text-slate-400 hover:text-slate-600 rounded-lg"
              >
                ✕
              </button>
            </div>

            <form onSubmit={handleSaveNewProduct} className="p-6 space-y-4">
              <div>
                <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1">
                  Product Name *
                </label>
                <input
                  type="text"
                  required
                  value={newProductName}
                  onChange={(e) => setNewProductName(e.target.value)}
                  className="w-full px-3 py-2 text-sm bg-white border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-600"
                />
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1">
                    Product SKU *
                  </label>
                  <input
                    type="text"
                    required
                    value={newProductSku}
                    onChange={(e) => setNewProductSku(e.target.value)}
                    className="w-full px-3 py-2 text-sm bg-white border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-600 font-mono"
                  />
                </div>

                <div>
                  <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1">
                    Category *
                  </label>
                  <select
                    required
                    value={newProductCategoryId}
                    onChange={(e) => setNewProductCategoryId(e.target.value)}
                    className="w-full px-3 py-2 text-sm bg-white border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-600"
                  >
                    <option value="">-- Choose Category --</option>
                    {categories.map((c) => (
                      <option key={c.id} value={c.id}>
                        {c.name}
                      </option>
                    ))}
                  </select>
                </div>
              </div>

              <div className="grid grid-cols-3 gap-3">
                <div>
                  <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1">
                    Store Unit
                  </label>
                  <select
                    value={newProductUnit}
                    onChange={(e) => setNewProductUnit(e.target.value)}
                    className="w-full px-3 py-2 text-sm bg-white border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-600"
                  >
                    <option value="Piece">Piece</option>
                    <option value="Kg">Kg</option>
                    <option value="Gram">Gram</option>
                    <option value="Litre">Litre</option>
                    <option value="Box">Box</option>
                    <option value="Packet">Packet</option>
                  </select>
                </div>

                <div>
                  <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1">
                    Purchase Price (₹)
                  </label>
                  <input
                    type="number"
                    step="0.01"
                    required
                    value={newProductPurchasePrice}
                    onChange={(e) => setNewProductPurchasePrice(e.target.value)}
                    className="w-full px-3 py-2 text-sm bg-white border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-600 font-mono"
                  />
                </div>

                <div>
                  <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1">
                    Selling Price (₹)
                  </label>
                  <input
                    type="number"
                    step="0.01"
                    required
                    value={newProductSellingPrice}
                    onChange={(e) => setNewProductSellingPrice(e.target.value)}
                    className="w-full px-3 py-2 text-sm bg-white border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-600 font-mono"
                  />
                </div>
              </div>

              <div className="grid grid-cols-2 gap-3 bg-slate-50 p-3 rounded-lg border border-slate-200">
                <div>
                  <label className="block text-xs font-semibold text-slate-700 mb-1">
                    Pack Size (e.g. 5 for 5KG)
                  </label>
                  <input
                    type="number"
                    step="any"
                    value={newProductPackSize}
                    onChange={(e) => {
                      setNewProductPackSize(e.target.value);
                      if (parseFloat(e.target.value) > 0) {
                        setNewProductConversion(parseFloat(e.target.value));
                      }
                    }}
                    placeholder="5"
                    className="w-full px-2.5 py-1.5 text-xs bg-white border border-slate-200 rounded-md"
                  />
                </div>
                <div>
                  <label className="block text-xs font-semibold text-slate-700 mb-1">
                    Conversion Multiplier
                  </label>
                  <input
                    type="number"
                    step="any"
                    value={newProductConversion}
                    onChange={(e) => setNewProductConversion(e.target.value)}
                    className="w-full px-2.5 py-1.5 text-xs bg-white border border-slate-200 rounded-md"
                  />
                </div>
              </div>

              <div className="pt-3 border-t border-slate-100 flex items-center justify-end gap-2">
                <button
                  type="button"
                  onClick={() => setNewProductModalItem(null)}
                  className="px-4 py-2 text-sm font-medium text-slate-700 bg-white border border-slate-200 rounded-lg hover:bg-slate-50"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={createProductMutation.isPending}
                  className="px-5 py-2 text-sm font-semibold text-white bg-indigo-600 hover:bg-indigo-700 rounded-lg shadow-sm flex items-center gap-2"
                >
                  {createProductMutation.isPending && <Loader2 className="w-4 h-4 animate-spin" />}
                  <span>Add to Inventory & Link</span>
                </button>
              </div>
            </form>
          </div>
        </div>,
        document.body
      )}

      {/* Comparison Modal */}
      <InvoiceComparisonModal
        isOpen={isCompareOpen}
        onClose={() => setIsCompareOpen(false)}
        invoiceId={id}
      />
    </div>
  );
}

function renderMatchBadge(status, confidence) {
  switch (status) {
    case 'ExactMatch':
      return (
        <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-bold bg-emerald-100 text-emerald-800">
          <Check className="w-3 h-3" /> Exact Match
        </span>
      );
    case 'HighConfidenceMatch':
      return (
        <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-bold bg-blue-100 text-blue-800">
          High Confidence ({Math.round((confidence || 0.9) * 100)}%)
        </span>
      );
    case 'PossibleMatch':
      return (
        <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-bold bg-amber-100 text-amber-800">
          Possible Match ({Math.round((confidence || 0.6) * 100)}%)
        </span>
      );
    case 'MultipleMatches':
      return (
        <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-bold bg-purple-100 text-purple-800">
          Multiple Candidates
        </span>
      );
    case 'UserConfirmed':
      return (
        <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-bold bg-teal-100 text-teal-800">
          <Check className="w-3 h-3" /> User Confirmed
        </span>
      );
    case 'NewProductCreated':
      return (
        <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-bold bg-indigo-100 text-indigo-800">
          <Plus className="w-3 h-3" /> New Product
        </span>
      );
    default:
      return (
        <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-bold bg-rose-100 text-rose-800">
          No Match
        </span>
      );
  }
}

export default InvoiceReviewPage;
