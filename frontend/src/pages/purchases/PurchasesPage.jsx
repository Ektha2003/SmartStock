import React, { useState, useMemo } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { Plus, Eye, Truck, FileText, Sparkles } from 'lucide-react';
import purchaseService from '../../services/purchaseService';
import PageHeader from '../../components/common/PageHeader';
import DataTable from '../../components/common/DataTable';
import SearchInput from '../../components/common/SearchInput';
import Pagination from '../../components/common/Pagination';
import ScanBillModal from '../../components/purchases/ScanBillModal';
import { formatCurrency, formatDate } from '../../utils/formatters';

export function PurchasesPage() {
  const navigate = useNavigate();

  const [search, setSearch] = useState('');
  const [currentPage, setCurrentPage] = useState(1);
  const [isScanModalOpen, setIsScanModalOpen] = useState(false);
  const pageSize = 8;

  const { data: purchasesData, isLoading } = useQuery({
    queryKey: ['purchases', currentPage, search],
    queryFn: () => purchaseService.getPurchases({ pageNumber: currentPage, pageSize }),
  });

  const purchases = purchasesData?.items || (Array.isArray(purchasesData) ? purchasesData : []);
  const totalCount = purchasesData?.totalCount || purchases.length;
  const totalPages = Math.ceil(totalCount / pageSize) || 1;

  const totalSpent = useMemo(() => {
    return purchases.reduce((sum, p) => sum + (p.totalAmount || 0), 0);
  }, [purchases]);

  const handleScanExtracted = (extracted) => {
    // Navigate to /purchases/new with prefilled data
    navigate('/purchases/new', { state: { prefillFromOcr: extracted } });
  };

  const columns = [
    {
      header: 'Supplier Bill / Invoice',
      key: 'invoiceNumber',
      render: (r) => (
        <div className="flex items-center gap-2">
          <div className="p-2 bg-blue-50 text-blue-700 rounded-lg">
            <FileText className="w-4 h-4" />
          </div>
          <div>
            <span className="font-bold text-slate-900 font-mono text-xs">{r.invoiceNumber}</span>
            <p className="text-[11px] text-slate-400">Date: {formatDate(r.purchaseDate || r.date)}</p>
          </div>
        </div>
      ),
    },
    {
      header: 'Supplier / Wholesaler',
      key: 'supplierName',
      render: (r) => (
        <div>
          <p className="font-semibold text-slate-900">{r.supplierName || 'Wholesale Depot'}</p>
          {r.notes && <span className="text-[11px] text-slate-400 truncate max-w-[150px] block">{r.notes}</span>}
        </div>
      ),
    },
    {
      header: 'SKUs Replenished',
      key: 'itemCount',
      align: 'center',
      render: (r) => (
        <span className="text-xs font-semibold bg-slate-100 text-slate-700 px-2 py-0.5 rounded-full">
          {r.items?.length || r.itemCount || 1} products
        </span>
      ),
    },
    {
      header: 'Total Cost Paid',
      key: 'totalAmount',
      align: 'right',
      render: (r) => (
        <span className="font-bold text-slate-900 text-sm">
          {formatCurrency(r.totalAmount)}
        </span>
      ),
    },
    {
      header: 'Action',
      key: 'actions',
      align: 'right',
      render: (r) => (
        <button
          onClick={(e) => {
            e.stopPropagation();
            navigate(`/purchases/${r.id}`);
          }}
          className="inline-flex items-center gap-1 px-3 py-1.5 text-xs font-semibold text-blue-700 bg-blue-50 hover:bg-blue-100 rounded-lg transition"
        >
          <Eye className="w-3.5 h-3.5" />
          <span>View Bill</span>
        </button>
      ),
    },
  ];

  return (
    <div className="space-y-5">
      <PageHeader
        title="Wholesale Purchases"
        subtitle="Track stock replenishment bills from mandi suppliers and FMCG distributors."
        breadcrumbs={[{ label: 'Purchases' }]}
        action={
          <div className="flex items-center gap-2">
            <button
              onClick={() => setIsScanModalOpen(true)}
              className="inline-flex items-center gap-1.5 px-3.5 py-2 bg-indigo-50 hover:bg-indigo-100 text-indigo-700 text-xs font-bold rounded-lg border border-indigo-200 transition"
            >
              <Sparkles className="w-4 h-4 text-amber-500" />
              <span>Scan Paper Bill (AI OCR)</span>
            </button>
            <Link
              to="/purchases/new"
              className="inline-flex items-center gap-2 px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white text-sm font-semibold rounded-lg shadow-sm transition"
            >
              <Truck className="w-4 h-4" />
              <span>Record Purchase</span>
            </Link>
          </div>
        }
      />

      <div className="bg-white p-4 rounded-xl border border-slate-200 flex flex-col sm:flex-row sm:items-center justify-between gap-4 shadow-xs">
        <div className="flex items-center gap-6">
          <div>
            <p className="text-xs font-semibold text-slate-500 uppercase tracking-wider">Total Inventory Sourced</p>
            <p className="text-xl font-bold text-slate-900">{formatCurrency(totalSpent)}</p>
          </div>
          <div className="h-8 w-px bg-slate-200 hidden sm:block" />
          <div>
            <p className="text-xs font-semibold text-slate-500 uppercase tracking-wider">Purchases Logged</p>
            <p className="text-xl font-bold text-slate-900">{totalCount} supplier bills</p>
          </div>
        </div>

        <SearchInput
          value={search}
          onChange={(val) => {
            setSearch(val);
            setCurrentPage(1);
          }}
          placeholder="Search by invoice # or supplier..."
        />
      </div>

      <DataTable
        columns={columns}
        data={purchases}
        isLoading={isLoading}
        onRowClick={(row) => navigate(`/purchases/${row.id}`)}
        emptyTitle="No purchases recorded yet"
        emptyDescription="Record your first stock delivery bill or scan a vendor paper invoice."
      />

      <Pagination
        currentPage={currentPage}
        totalPages={totalPages}
        onPageChange={setCurrentPage}
      />

      {/* AI Invoice OCR Scanner Modal */}
      <ScanBillModal
        isOpen={isScanModalOpen}
        onClose={() => setIsScanModalOpen(false)}
        onExtracted={handleScanExtracted}
      />
    </div>
  );
}

export default PurchasesPage;
