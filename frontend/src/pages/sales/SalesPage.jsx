import React, { useState, useMemo } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { Plus, Eye, ShoppingCart, IndianRupee, FileText } from 'lucide-react';
import salesService from '../../services/salesService';
import PageHeader from '../../components/common/PageHeader';
import DataTable from '../../components/common/DataTable';
import SearchInput from '../../components/common/SearchInput';
import FilterDropdown from '../../components/common/FilterDropdown';
import { formatCurrency, formatDateTime } from '../../utils/formatters';

export function SalesPage() {
  const navigate = useNavigate();

  const [search, setSearch] = useState('');
  const [paymentFilter, setPaymentFilter] = useState('');
  const [currentPage, setCurrentPage] = useState(1);
  const pageSize = 8;

  const { data: salesData, isLoading } = useQuery({
    queryKey: ['sales'],
    queryFn: () => salesService.getSales(),
  });

  const sales = useMemo(() => {
    if (Array.isArray(salesData)) return salesData;
    if (salesData?.items && Array.isArray(salesData.items)) return salesData.items;
    return [];
  }, [salesData]);

  const filteredSales = useMemo(() => {
    return (sales || []).filter((s) => {
      const invoiceNo = (s.invoiceNumber || '').toLowerCase();
      const custName = (s.customerName || '').toLowerCase();
      const custPhone = s.customerPhone || '';
      const searchLower = search.toLowerCase();

      const matchSearch =
        !search ||
        invoiceNo.includes(searchLower) ||
        custName.includes(searchLower) ||
        custPhone.includes(search);
      const matchPayment = !paymentFilter || s.paymentMethod === paymentFilter;
      return matchSearch && matchPayment;
    });
  }, [sales, search, paymentFilter]);

  const totalPages = Math.ceil(filteredSales.length / pageSize) || 1;
  const paginatedData = useMemo(() => {
    const start = (currentPage - 1) * pageSize;
    return filteredSales.slice(start, start + pageSize);
  }, [filteredSales, currentPage, pageSize]);

  const totalRevenue = useMemo(() => {
    return (sales || []).reduce((sum, s) => sum + (Number(s.totalAmount) || 0), 0);
  }, [sales]);

  const columns = [
    {
      header: 'Invoice #',
      key: 'invoiceNumber',
      render: (r) => (
        <div className="flex items-center gap-2">
          <div className="p-2 bg-indigo-50 text-indigo-700 rounded-lg">
            <FileText className="w-4 h-4" />
          </div>
          <div>
            <span className="font-bold text-slate-900 font-mono text-xs">{r.invoiceNumber}</span>
            <p className="text-[11px] text-slate-400">{formatDateTime(r.date)}</p>
          </div>
        </div>
      ),
    },
    {
      header: 'Customer',
      key: 'customerName',
      render: (r) => (
        <div>
          <p className="font-semibold text-slate-900">{r.customerName || 'Walk-in Customer'}</p>
          {r.customerPhone && <span className="text-[11px] text-slate-500">{r.customerPhone}</span>}
        </div>
      ),
    },
    {
      header: 'Items Count',
      key: 'itemCount',
      align: 'center',
      render: (r) => (
        <span className="text-xs font-semibold bg-slate-100 text-slate-700 px-2 py-0.5 rounded-full">
          {r.items?.length || r.itemCount || 1} items
        </span>
      ),
    },
    {
      header: 'Payment Mode',
      key: 'paymentMethod',
      align: 'center',
      render: (r) => (
        <span
          className={`text-xs font-semibold px-2.5 py-0.5 rounded-full ${r.paymentMethod === 'UPI'
              ? 'bg-purple-50 text-purple-700 border border-purple-200'
              : r.paymentMethod === 'Cash'
                ? 'bg-emerald-50 text-emerald-700 border border-emerald-200'
                : 'bg-blue-50 text-blue-700 border border-blue-200'
            }`}
        >
          {r.paymentMethod || 'Cash'}
        </span>
      ),
    },
    {
      header: 'Total Bill Amount',
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
            navigate(`/sales/${r.id || r.invoiceNumber}`);
          }}
          className="inline-flex items-center gap-1 px-3 py-1.5 text-xs font-semibold text-indigo-700 bg-indigo-50 hover:bg-indigo-100 rounded-lg transition"
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
        title="Sales & Billing"
        subtitle="Manage customer invoices, cash counter sales, and digital UPI receipts."
        breadcrumbs={[{ label: 'Sales' }]}
        action={
          <Link
            to="/sales/new"
            className="inline-flex items-center gap-2 px-4 py-2 bg-emerald-600 hover:bg-emerald-700 text-white text-sm font-semibold rounded-lg shadow-sm transition"
          >
            <ShoppingCart className="w-4 h-4" />
            <span>New Sale (POS)</span>
          </Link>
        }
      />

      {/* Top summary banner */}
      <div className="bg-white p-4 rounded-xl border border-slate-200 flex flex-col sm:flex-row sm:items-center justify-between gap-4 shadow-xs">
        <div className="flex items-center gap-6">
          <div>
            <p className="text-xs font-semibold text-slate-500 uppercase tracking-wider">Total Recorded Sales</p>
            <p className="text-xl font-bold text-slate-900">{formatCurrency(totalRevenue)}</p>
          </div>
          <div className="h-8 w-px bg-slate-200 hidden sm:block" />
          <div>
            <p className="text-xs font-semibold text-slate-500 uppercase tracking-wider">Invoices Issued</p>
            <p className="text-xl font-bold text-slate-900">{sales.length} bills</p>
          </div>
        </div>

        <div className="flex flex-col sm:flex-row sm:items-center gap-3">
          <SearchInput
            value={search}
            onChange={(val) => {
              setSearch(val);
              setCurrentPage(1);
            }}
            placeholder="Search by invoice or customer..."
          />
          <FilterDropdown
            label="All Payments"
            value={paymentFilter}
            options={[
              { label: 'UPI', value: 'UPI' },
              { label: 'Cash', value: 'Cash' },
              { label: 'Card', value: 'Card' },
              { label: 'Credit / Udhaar', value: 'Credit / Udhaar' },
            ]}
            onChange={(val) => {
              setPaymentFilter(val);
              setCurrentPage(1);
            }}
          />
        </div>
      </div>

      <DataTable
        columns={columns}
        data={paginatedData}
        isLoading={isLoading}
        onRowClick={(row) => navigate(`/sales/${row.id || row.invoiceNumber}`)}
        emptyTitle="No sales found"
        emptyDescription="Click 'New Sale (POS)' to record your store's first customer sale."
        pagination={{
          currentPage,
          totalPages,
          onPageChange: setCurrentPage,
          totalCount: filteredSales.length,
          pageSize,
        }}
      />
    </div>
  );
}

export default SalesPage;

