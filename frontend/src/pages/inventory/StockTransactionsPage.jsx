import React, { useState } from 'react';
import { useQuery, keepPreviousData } from '@tanstack/react-query';
import { ArrowLeft, ArrowDownLeft, ArrowUpRight, SlidersHorizontal, Plus } from 'lucide-react';
import { Link } from 'react-router-dom';
import inventoryService from '../../services/inventoryService';
import PageHeader from '../../components/common/PageHeader';
import DataTable from '../../components/common/DataTable';
import SearchInput from '../../components/common/SearchInput';
import FilterDropdown from '../../components/common/FilterDropdown';
import { formatDateTime } from '../../utils/formatters';
import { TransactionType } from '../../utils/constants';

export function StockTransactionsPage() {
  const [search, setSearch] = useState('');
  const [typeFilter, setTypeFilter] = useState('');
  const [currentPage, setCurrentPage] = useState(1);
  const pageSize = 10;

  const { data: transactionsData, isLoading } = useQuery({
    queryKey: ['inventory', 'transactions', currentPage, typeFilter, search],
    queryFn: () =>
      inventoryService.getTransactions({
        pageNumber: currentPage,
        pageSize,
        type: typeFilter ? Number(typeFilter) : undefined,
        search: search.trim() || undefined,
      }),
    placeholderData: keepPreviousData,
  });

  const transactions = transactionsData?.items || (Array.isArray(transactionsData) ? transactionsData : []);
  const totalCount = transactionsData?.totalCount || transactions.length;

  const totalPages = Math.ceil(totalCount / pageSize) || 1;

  const getTypeBadge = (type, typeNum) => {
    if (type === 'Sale' || typeNum === TransactionType.Sale) {
      return (
        <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-semibold bg-rose-50 text-rose-700 border border-rose-200">
          <ArrowDownLeft className="w-3 h-3" /> Sale Out
        </span>
      );
    }
    if (type === 'Purchase' || typeNum === TransactionType.Purchase) {
      return (
        <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-semibold bg-emerald-50 text-emerald-700 border border-emerald-200">
          <ArrowUpRight className="w-3 h-3" /> Purchase In
        </span>
      );
    }
    if (type === 'Opening' || typeNum === TransactionType.Opening) {
      return (
        <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-semibold bg-blue-50 text-blue-700 border border-blue-200">
          <Plus className="w-3 h-3" /> Opening Stock
        </span>
      );
    }
    return (
      <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-semibold bg-amber-50 text-amber-700 border border-amber-200">
        <SlidersHorizontal className="w-3 h-3" /> Adjustment
      </span>
    );
  };

  const columns = [
    {
      header: 'Date & Time',
      key: 'date',
      render: (r) => (
        <span className="text-slate-600 text-xs font-medium">
          {formatDateTime(r.date || r.createdAt)}
        </span>
      ),
    },
    {
      header: 'Product Name',
      key: 'productName',
      render: (r) => <span className="font-semibold text-slate-900">{r.productName}</span>,
    },
    {
      header: 'Transaction Type',
      key: 'type',
      render: (r) => getTypeBadge(r.transactionTypeName || r.type, r.transactionType),
    },
    {
      header: 'Qty Change',
      key: 'quantity',
      align: 'center',
      render: (r) => (
        <span
          className={`font-bold ${
            r.quantity > 0 ? 'text-emerald-600' : 'text-slate-900'
          }`}
        >
          {r.quantity > 0 ? `+${r.quantity}` : r.quantity}
        </span>
      ),
    },
    {
      header: 'Prev Stock',
      key: 'previousStock',
      align: 'center',
      render: (r) => <span className="text-slate-500">{r.previousStock}</span>,
    },
    {
      header: 'New Balance',
      key: 'newStock',
      align: 'center',
      render: (r) => <span className="font-bold text-slate-900">{r.newStock}</span>,
    },
    {
      header: 'Audit Reference / Notes',
      key: 'reference',
      render: (r) => (
        <span className="text-xs text-slate-600 font-mono bg-slate-100 px-2 py-0.5 rounded max-w-[200px] truncate block" title={r.notes || r.reference}>
          {r.notes || r.reference || r.referenceId || '-'}
        </span>
      ),
    },
  ];

  return (
    <div className="space-y-5">
      <PageHeader
        title="Stock Movement Ledger"
        subtitle="Complete audited stock ledger of all inventory additions, sales deductions, and adjustments."
        breadcrumbs={[
          { label: 'Inventory', to: '/inventory' },
          { label: 'Stock Transactions' },
        ]}
        action={
          <Link
            to="/inventory"
            className="inline-flex items-center gap-1.5 px-3.5 py-1.5 text-xs font-semibold text-slate-700 bg-white border border-slate-200 rounded-lg hover:bg-slate-50 transition"
          >
            <ArrowLeft className="w-3.5 h-3.5" /> Back to Inventory
          </Link>
        }
      />

      <div className="bg-white p-4 rounded-xl border border-slate-200 flex flex-col sm:flex-row sm:items-center justify-between gap-3 shadow-xs">
        <div className="flex flex-col sm:flex-row sm:items-center gap-3 flex-1">
          <SearchInput
            value={search}
            onChange={(val) => {
              setSearch(val);
              setCurrentPage(1);
            }}
            placeholder="Search by product name or reference..."
          />
          <FilterDropdown
            label="All Types"
            value={typeFilter}
            options={[
              { label: 'Opening Stock', value: '1' },
              { label: 'Purchases (Inward)', value: '2' },
              { label: 'Sales (Deduction)', value: '3' },
              { label: 'Adjustments (Manual)', value: '4' },
            ]}
            onChange={(val) => {
              setTypeFilter(val);
              setCurrentPage(1);
            }}
          />
        </div>

        <div className="text-xs text-slate-500 font-medium">
          Total Movements: <span className="font-semibold text-slate-900">{totalCount}</span>
        </div>
      </div>

      <DataTable
        columns={columns}
        data={transactions}
        isLoading={isLoading}
        emptyTitle="No stock movements found"
        emptyDescription="Transactions will show up here as products are sold or replenished."
        pagination={{ currentPage, totalPages, onPageChange: setCurrentPage, totalCount, pageSize }}
      />
    </div>
  );
}

export default StockTransactionsPage;
