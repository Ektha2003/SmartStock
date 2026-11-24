import React, { useState, useMemo } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Link, useSearchParams } from 'react-router-dom';
import {
  FileSpreadsheet,
  Upload,
  Plus,
  Search,
  Filter,
  CheckCircle2,
  Clock,
  AlertTriangle,
  TrendingUp,
  FileText,
  Building2,
  X,
  Loader2,
  BotMessageSquare,
  Sparkles,
  ArrowRight
} from 'lucide-react';
import supplierInvoiceService from '../../services/supplierInvoiceService';
import supplierService from '../../services/supplierService';
import PageHeader from '../../components/common/PageHeader';
import DataTable from '../../components/common/DataTable';
import SearchInput from '../../components/common/SearchInput';
import Pagination from '../../components/common/Pagination';
import StatusBadge from '../../components/common/StatusBadge';
import AIInvoiceAssistantDrawer from '../../components/invoices/AIInvoiceAssistantDrawer';
import { formatCurrency, formatDate } from '../../utils/formatters';
import { toast } from 'sonner';

export function SupplierInvoicesPage() {
  const queryClient = useQueryClient();
  const [searchParams] = useSearchParams();
  const initialSupplierId = searchParams.get('supplierId') || '';

  const [search, setSearch] = useState('');
  const [selectedStatus, setSelectedStatus] = useState('');
  const [selectedSupplierId, setSelectedSupplierId] = useState(initialSupplierId);
  const [onlyPriceHikes, setOnlyPriceHikes] = useState(false);
  const [currentPage, setCurrentPage] = useState(1);
  const pageSize = 10;

  const [isUploadModalOpen, setIsUploadModalOpen] = useState(false);
  const [isAssistantOpen, setIsAssistantOpen] = useState(false);
  const [uploadFile, setUploadFile] = useState(null);
  const [uploadSupplierId, setUploadSupplierId] = useState('');

  // Fetch KPI Dashboard Metrics
  const { data: metrics } = useQuery({
    queryKey: ['supplier-invoices-metrics'],
    queryFn: () => supplierInvoiceService.getDashboardMetrics(),
  });

  // Fetch Invoices
  const { data: invoicesData, isLoading } = useQuery({
    queryKey: ['supplier-invoices', selectedStatus, selectedSupplierId, onlyPriceHikes, currentPage, search],
    queryFn: () =>
      supplierInvoiceService.getInvoices({
        status: selectedStatus || undefined,
        supplierId: selectedSupplierId || undefined,
        hasPriceIncreases: onlyPriceHikes || undefined,
        pageNumber: currentPage,
        pageSize,
        search: search || undefined,
      }),
  });

  // Fetch Suppliers for dropdowns
  const { data: suppliersData } = useQuery({
    queryKey: ['suppliers'],
    queryFn: () => supplierService.getSuppliers(),
  });

  const suppliers = useMemo(() => {
    if (Array.isArray(suppliersData)) return suppliersData;
    if (suppliersData?.items && Array.isArray(suppliersData.items)) return suppliersData.items;
    return [];
  }, [suppliersData]);

  const invoices = invoicesData?.items || [];
  const totalCount = invoicesData?.totalCount || 0;
  const totalPages = Math.ceil(totalCount / pageSize) || 1;

  // Upload Mutation
  const uploadMutation = useMutation({
    mutationFn: ({ file, supplierId }) => supplierInvoiceService.uploadInvoice(file, supplierId),
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: ['supplier-invoices'] });
      queryClient.invalidateQueries({ queryKey: ['supplier-invoices-metrics'] });
      setIsUploadModalOpen(false);
      setUploadFile(null);
      setUploadSupplierId('');
      if (data?.isDuplicate) {
        toast.warning(`Invoice uploaded but flagged as potential duplicate: #${data.invoiceNumber}`);
      } else {
        toast.success(`Invoice #${data.invoiceNumber} uploaded & scanned! Ready for review.`);
      }
    },
    onError: (err) => toast.error(err.message || 'Failed to upload invoice'),
  });

  const handleUploadSubmit = (e) => {
    e.preventDefault();
    if (!uploadFile) {
      toast.error('Please select an invoice document (PDF, PNG, or JPG)');
      return;
    }
    uploadMutation.mutate({
      file: uploadFile,
      supplierId: uploadSupplierId || null,
    });
  };

  const columns = [
    {
      header: 'Invoice Number',
      key: 'invoiceNumber',
      render: (r) => (
        <div className="flex items-center gap-2.5">
          <div className={`p-2 rounded-lg ${r.isDuplicate ? 'bg-rose-50 text-rose-600' : 'bg-indigo-50 text-indigo-700'}`}>
            <FileSpreadsheet className="w-4 h-4" />
          </div>
          <div>
            <Link
              to={`/supplier-invoices/${r.id}/review`}
              className="font-bold text-slate-900 hover:text-indigo-600 transition"
            >
              #{r.invoiceNumber}
            </Link>
            <p className="text-[11px] text-slate-400">{r.originalFileName}</p>
          </div>
        </div>
      ),
    },
    {
      header: 'Supplier',
      key: 'supplierName',
      render: (r) => (
        <div>
          <span className="font-semibold text-slate-800">{r.supplierName || r.supplierNameRaw || 'Unassigned'}</span>
          {r.supplierGst && <p className="text-[11px] text-slate-400 font-mono">GST: {r.supplierGst}</p>}
        </div>
      ),
    },
    {
      header: 'Invoice Date',
      key: 'invoiceDate',
      render: (r) => (
        <div className="text-xs text-slate-600">
          <p className="font-medium text-slate-800">{formatDate(r.invoiceDate)}</p>
          {r.dueDate && <p className="text-[11px] text-slate-400">Due: {formatDate(r.dueDate)}</p>}
        </div>
      ),
    },
    {
      header: 'Items & Sync',
      key: 'items',
      align: 'center',
      render: (r) => (
        <div className="space-y-1 text-xs">
          <span className="font-semibold text-slate-800">{r.itemsCount} Items</span>
          <div className="flex items-center justify-center gap-1.5 text-[11px]">
            <span className="text-emerald-700 font-medium">{r.matchedItemsCount} matched</span>
            {r.unmatchedItemsCount > 0 && (
              <span className="text-rose-600 font-bold">• {r.unmatchedItemsCount} unlinked</span>
            )}
          </div>
        </div>
      ),
    },
    {
      header: 'Grand Total',
      key: 'grandTotal',
      align: 'right',
      render: (r) => (
        <div className="text-right">
          <span className="font-bold text-slate-900 font-mono text-sm">{formatCurrency(r.grandTotal)}</span>
          {r.priceHikeCount > 0 && (
            <div className="flex items-center justify-end gap-1 text-[11px] text-rose-600 font-semibold mt-0.5">
              <TrendingUp className="w-3 h-3" />
              <span>{r.priceHikeCount} price hikes</span>
            </div>
          )}
        </div>
      ),
    },
    {
      header: 'Status',
      key: 'status',
      align: 'center',
      render: (r) => (
        <div className="flex flex-col items-center gap-1">
          <StatusBadge status={r.status} size="sm" />
          {r.isDuplicate && (
            <span className="text-[10px] font-bold text-rose-700 bg-rose-50 px-1.5 py-0.5 rounded border border-rose-200">
              Duplicate
            </span>
          )}
        </div>
      ),
    },
    {
      header: 'Actions',
      key: 'actions',
      align: 'right',
      render: (r) => (
        <div className="flex items-center justify-end gap-2">
          <Link
            to={`/supplier-invoices/${r.id}/review`}
            className={`inline-flex items-center gap-1 px-3 py-1.5 text-xs font-semibold rounded-lg shadow-xs transition ${
              r.status === 'ReadyForReview'
                ? 'bg-indigo-600 hover:bg-indigo-700 text-white'
                : 'bg-slate-100 hover:bg-slate-200 text-slate-800'
            }`}
          >
            <span>{r.status === 'ReadyForReview' ? 'Review & Match' : 'View Details'}</span>
            <ArrowRight className="w-3 h-3" />
          </Link>
        </div>
      ),
    },
  ];

  return (
    <div className="space-y-6">
      <PageHeader
        title="Supplier Invoices & OCR Automation"
        subtitle="Upload vendor bills, auto-extract items with Gemini AI, match products, and sync inventory."
        breadcrumbs={[{ label: 'Supplier Invoices' }]}
        action={
          <div className="flex items-center gap-2">
            <button
              onClick={() => setIsAssistantOpen(true)}
              className="inline-flex items-center gap-2 px-3.5 py-2 bg-indigo-50 text-indigo-700 hover:bg-indigo-100 text-sm font-semibold rounded-lg transition"
            >
              <Sparkles className="w-4 h-4 text-indigo-600" />
              <span>AI Assistant</span>
            </button>
            <button
              onClick={() => setIsUploadModalOpen(true)}
              className="inline-flex items-center gap-2 px-4 py-2 bg-indigo-600 hover:bg-indigo-700 text-white text-sm font-semibold rounded-lg shadow-sm transition"
            >
              <Upload className="w-4 h-4" />
              <span>Upload Invoice</span>
            </button>
          </div>
        }
      />

      {/* KPI Dashboard Metrics Cards */}
      <div className="grid grid-cols-2 lg:grid-cols-5 gap-4">
        <div className="bg-white p-4 rounded-xl border border-slate-200 shadow-xs">
          <p className="text-xs font-semibold text-slate-400 uppercase tracking-wider">Total Invoices</p>
          <p className="text-2xl font-bold text-slate-900 mt-1">{metrics?.totalInvoices ?? 0}</p>
          <p className="text-[11px] text-slate-500 mt-0.5">Scanned documents</p>
        </div>

        <div className="bg-white p-4 rounded-xl border border-indigo-200/80 shadow-xs bg-indigo-50/20">
          <p className="text-xs font-semibold text-indigo-600 uppercase tracking-wider">Ready for Review</p>
          <p className="text-2xl font-bold text-indigo-700 mt-1">{metrics?.readyForReviewCount ?? 0}</p>
          <p className="text-[11px] text-indigo-600 mt-0.5">Pending stock sync</p>
        </div>

        <div className="bg-white p-4 rounded-xl border border-emerald-200/80 shadow-xs bg-emerald-50/20">
          <p className="text-xs font-semibold text-emerald-600 uppercase tracking-wider">Approved & Synced</p>
          <p className="text-2xl font-bold text-emerald-700 mt-1">{metrics?.approvedCount ?? 0}</p>
          <p className="text-[11px] text-emerald-600 mt-0.5">Credited to inventory</p>
        </div>

        <div className="bg-white p-4 rounded-xl border border-rose-200/80 shadow-xs bg-rose-50/20">
          <p className="text-xs font-semibold text-rose-600 uppercase tracking-wider">Price Hikes Detected</p>
          <p className="text-2xl font-bold text-rose-700 mt-1">{metrics?.totalPriceHikesDetected ?? 0}</p>
          <p className="text-[11px] text-rose-600 mt-0.5">Products with higher rate</p>
        </div>

        <div className="bg-white p-4 rounded-xl border border-slate-200 shadow-xs col-span-2 lg:col-span-1">
          <p className="text-xs font-semibold text-slate-400 uppercase tracking-wider">Spend This Month</p>
          <p className="text-xl font-bold text-slate-900 mt-1 font-mono">
            {formatCurrency(metrics?.totalAmountThisMonth ?? 0)}
          </p>
          <p className="text-[11px] text-slate-500 mt-0.5">Approved purchases</p>
        </div>
      </div>

      {/* Filter and Search Bar */}
      <div className="bg-white p-4 rounded-xl border border-slate-200 shadow-xs space-y-3">
        <div className="flex flex-col md:flex-row md:items-center justify-between gap-3">
          <div className="w-full md:w-80">
            <SearchInput
              value={search}
              onChange={(e) => {
                setSearch(e.target.value);
                setCurrentPage(1);
              }}
              placeholder="Search invoice number, supplier..."
            />
          </div>

          <div className="flex flex-wrap items-center gap-2">
            <select
              value={selectedSupplierId}
              onChange={(e) => {
                setSelectedSupplierId(e.target.value);
                setCurrentPage(1);
              }}
              className="text-xs px-3 py-2 bg-slate-50 border border-slate-200 rounded-lg text-slate-700 font-medium"
            >
              <option value="">All Suppliers</option>
              {suppliers.map((s) => (
                <option key={s.id} value={s.id}>
                  {s.name}
                </option>
              ))}
            </select>

            <select
              value={selectedStatus}
              onChange={(e) => {
                setSelectedStatus(e.target.value);
                setCurrentPage(1);
              }}
              className="text-xs px-3 py-2 bg-slate-50 border border-slate-200 rounded-lg text-slate-700 font-medium"
            >
              <option value="">All Statuses</option>
              <option value="ReadyForReview">Ready For Review</option>
              <option value="Approved">Approved</option>
              <option value="Duplicate">Duplicate</option>
              <option value="Rejected">Rejected</option>
            </select>

            <button
              onClick={() => {
                setOnlyPriceHikes(!onlyPriceHikes);
                setCurrentPage(1);
              }}
              className={`text-xs px-3 py-2 rounded-lg font-semibold flex items-center gap-1.5 transition ${
                onlyPriceHikes
                  ? 'bg-rose-100 text-rose-800 border border-rose-300'
                  : 'bg-slate-50 border border-slate-200 text-slate-600 hover:bg-slate-100'
              }`}
            >
              <TrendingUp className="w-3.5 h-3.5" />
              <span>Price Hikes Only</span>
            </button>
          </div>
        </div>
      </div>

      {/* Main Invoices Table */}
      <DataTable
        columns={columns}
        data={invoices}
        isLoading={isLoading}
        emptyMessage="No supplier invoices found matching current filters. Click 'Upload Invoice' to process a vendor bill."
      />

      {totalPages > 1 && (
        <Pagination
          currentPage={currentPage}
          totalPages={totalPages}
          onPageChange={setCurrentPage}
        />
      )}

      {/* Upload Invoice Modal */}
      {isUploadModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/40 backdrop-blur-xs">
          <div className="bg-white rounded-2xl border border-slate-200 shadow-xl w-full max-w-lg overflow-hidden flex flex-col">
            <div className="px-6 py-4 border-b border-slate-100 flex items-center justify-between bg-slate-50/50">
              <h3 className="font-bold text-slate-900 text-base flex items-center gap-2">
                <Upload className="w-5 h-5 text-indigo-600" />
                Upload Supplier Invoice
              </h3>
              <button
                onClick={() => setIsUploadModalOpen(false)}
                className="p-1 text-slate-400 hover:text-slate-600 rounded-lg"
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            <form onSubmit={handleUploadSubmit} className="p-6 space-y-4">
              <div>
                <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1">
                  Supplier / Vendor (Optional)
                </label>
                <select
                  value={uploadSupplierId}
                  onChange={(e) => setUploadSupplierId(e.target.value)}
                  className="w-full px-3 py-2 text-sm bg-white border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-600"
                >
                  <option value="">-- Auto-Detect From Invoice --</option>
                  {suppliers.map((s) => (
                    <option key={s.id} value={s.id}>
                      {s.name} {s.gstin ? `(${s.gstin})` : ''}
                    </option>
                  ))}
                </select>
                <p className="text-[11px] text-slate-400 mt-1">
                  If left empty, AI will automatically match or link the supplier via GSTIN or legal name.
                </p>
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1">
                  Invoice Document (PDF, JPG, PNG) *
                </label>
                <div className="border-2 border-dashed border-slate-200 hover:border-indigo-500 rounded-xl p-6 text-center cursor-pointer transition bg-slate-50/50">
                  <input
                    type="file"
                    id="invoiceFileInput"
                    accept=".pdf,image/png,image/jpeg,image/jpg"
                    onChange={(e) => setUploadFile(e.target.files?.[0] || null)}
                    className="hidden"
                  />
                  <label htmlFor="invoiceFileInput" className="cursor-pointer">
                    <FileSpreadsheet className="w-10 h-10 text-indigo-500 mx-auto mb-2" />
                    {uploadFile ? (
                      <div>
                        <p className="font-bold text-sm text-slate-900">{uploadFile.name}</p>
                        <p className="text-xs text-slate-400 mt-0.5">
                          {(uploadFile.size / 1024 / 1024).toFixed(2)} MB • Click to change file
                        </p>
                      </div>
                    ) : (
                      <div>
                        <p className="text-sm font-semibold text-slate-800">
                          Click to select invoice file
                        </p>
                        <p className="text-xs text-slate-400 mt-1">
                          Supports PDF, JPG, PNG scanned supplier bills (multi-page invoices supported)
                        </p>
                      </div>
                    )}
                  </label>
                </div>
              </div>

              <div className="bg-indigo-50/70 p-3 rounded-lg border border-indigo-200/60 text-xs text-indigo-900">
                <span className="font-bold">What happens next?</span> Gemini OCR will parse all line items, tax numbers, and calculate pack size unit conversions before presenting an interactive approval table.
              </div>

              <div className="pt-3 border-t border-slate-100 flex items-center justify-end gap-2">
                <button
                  type="button"
                  onClick={() => setIsUploadModalOpen(false)}
                  className="px-4 py-2 text-sm font-medium text-slate-700 bg-white border border-slate-200 rounded-lg hover:bg-slate-50"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={uploadMutation.isPending || !uploadFile}
                  className="px-5 py-2 text-sm font-bold text-white bg-indigo-600 hover:bg-indigo-700 disabled:opacity-50 rounded-lg shadow-sm flex items-center gap-2"
                >
                  {uploadMutation.isPending && <Loader2 className="w-4 h-4 animate-spin" />}
                  <span>{uploadMutation.isPending ? 'Extracting with AI...' : 'Upload & Process'}</span>
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* AI Assistant Drawer */}
      <AIInvoiceAssistantDrawer
        isOpen={isAssistantOpen}
        onClose={() => setIsAssistantOpen(false)}
      />
    </div>
  );
}

export default SupplierInvoicesPage;
