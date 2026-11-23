import React, { useState, useMemo } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import {
  Boxes,
  IndianRupee,
  AlertTriangle,
  PackageX,
  History,
  SlidersHorizontal,
  Eye,
} from 'lucide-react';
import inventoryService from '../../services/inventoryService';
import productService from '../../services/productService';
import PageHeader from '../../components/common/PageHeader';
import StatCard from '../../components/common/StatCard';
import DataTable from '../../components/common/DataTable';
import SearchInput from '../../components/common/SearchInput';
import FilterDropdown from '../../components/common/FilterDropdown';
import StatusBadge from '../../components/common/StatusBadge';
import Pagination from '../../components/common/Pagination';
import StockAdjustmentModal from '../../components/inventory/StockAdjustmentModal';
import { formatCurrency, formatNumber } from '../../utils/formatters';

export function InventoryPage() {
  const navigate = useNavigate();

  const [search, setSearch] = useState('');
  const [stockFilter, setStockFilter] = useState('');
  const [currentPage, setCurrentPage] = useState(1);
  const pageSize = 8;

  const [adjustProduct, setAdjustProduct] = useState(null);
  const [isAdjustOpen, setIsAdjustOpen] = useState(false);

  const { data: summary, isLoading: isSummaryLoading } = useQuery({
    queryKey: ['inventory', 'summary'],
    queryFn: () => inventoryService.getSummary(),
  });

  const { data: productsData, isLoading: isProductsLoading } = useQuery({
    queryKey: ['products'],
    queryFn: () => productService.getProducts({ pageSize: 100 }),
  });

  const products = productsData?.items || (Array.isArray(productsData) ? productsData : []);

  const filteredProducts = useMemo(() => {
    return products.filter((p) => {
      const q = (search || '').toLowerCase();
      const matchSearch =
        !search ||
        (p.name || '').toLowerCase().includes(q) ||
        (p.sku || '').toLowerCase().includes(q);
      const minStock = p.minimumStock ?? p.minStock ?? 5;
      let matchStatus = true;
      if (stockFilter === 'in') matchStatus = p.currentStock > minStock;
      else if (stockFilter === 'low') matchStatus = p.currentStock <= minStock && p.currentStock > 0;
      else if (stockFilter === 'out') matchStatus = p.currentStock === 0;

      return matchSearch && matchStatus;
    });
  }, [products, search, stockFilter]);

  const totalPages = Math.ceil(filteredProducts.length / pageSize) || 1;
  const paginatedData = useMemo(() => {
    const start = (currentPage - 1) * pageSize;
    return filteredProducts.slice(start, start + pageSize);
  }, [filteredProducts, currentPage, pageSize]);

  const columns = [
    {
      header: 'Product Details',
      key: 'name',
      render: (r) => (
        <div>
          <p className="font-semibold text-slate-900">{r.name}</p>
          <span className="text-xs text-slate-500 font-mono">
            {r.sku} • {r.categoryName || 'Staples'}
          </span>
        </div>
      ),
    },
    {
      header: 'Current Stock',
      key: 'currentStock',
      align: 'center',
      render: (r) => {
        const minStock = r.minimumStock ?? r.minStock ?? 5;
        return (
          <span
            className={`font-bold ${
              r.currentStock === 0
                ? 'text-rose-600'
                : r.currentStock <= minStock
                ? 'text-amber-600'
                : 'text-slate-900'
            }`}
          >
            {r.currentStock} {r.unitName || ''}
          </span>
        );
      },
    },
    {
      header: 'Safety Threshold',
      key: 'minStock',
      align: 'center',
      render: (r) => <span className="text-slate-600 text-xs">{(r.minimumStock ?? r.minStock)} {r.unitName || ''}</span>,
    },
    {
      header: 'Stock Status',
      key: 'status',
      align: 'center',
      render: (r) => {
        const minStock = r.minimumStock ?? r.minStock ?? 5;
        let status = 'In Stock';
        if (r.currentStock === 0) status = 'Out of Stock';
        else if (r.currentStock <= minStock) status = 'Low Stock';
        return <StatusBadge status={status} size="sm" />;
      },
    },
    {
      header: 'Stock Valuation',
      key: 'valuation',
      align: 'right',
      render: (r) => (
        <div>
          <p className="font-semibold text-slate-900">
            {formatCurrency(r.currentStock * r.purchasePrice)}
          </p>
          <span className="text-[11px] text-slate-400">@{formatCurrency(r.purchasePrice)}/unit</span>
        </div>
      ),
    },
    {
      header: 'Actions',
      key: 'actions',
      align: 'right',
      render: (r) => (
        <div className="flex items-center justify-end gap-1" onClick={(e) => e.stopPropagation()}>
          <button
            onClick={() => {
              setAdjustProduct(r);
              setIsAdjustOpen(true);
            }}
            className="inline-flex items-center gap-1 px-2.5 py-1 text-xs font-semibold text-slate-700 bg-white hover:bg-slate-50 border border-slate-200 rounded-lg transition"
            title="Adjust Stock"
          >
            <SlidersHorizontal className="w-3.5 h-3.5 text-amber-600" />
            <span>Adjust</span>
          </button>
          <button
            onClick={() => navigate(`/products/${r.id}`)}
            className="p-1.5 text-slate-500 hover:text-indigo-600 hover:bg-slate-100 rounded-md transition"
            title="View History"
          >
            <Eye className="w-4 h-4" />
          </button>
        </div>
      ),
    },
  ];

  return (
    <div className="space-y-6">
      <PageHeader
        title="Inventory Overview"
        subtitle="Real-time stock valuation, unit counts, and shelf replenishment levels."
        breadcrumbs={[{ label: 'Inventory' }]}
        action={
          <div className="flex items-center gap-2">
            <Link
              to="/inventory/transactions"
              className="inline-flex items-center gap-1.5 px-3.5 py-2 text-xs font-semibold text-slate-700 bg-white border border-slate-200 rounded-lg hover:bg-slate-50 transition"
            >
              <History className="w-3.5 h-3.5" />
              <span>Stock Transactions</span>
            </Link>
            <button
              onClick={() => {
                setAdjustProduct(null);
                setIsAdjustOpen(true);
              }}
              className="inline-flex items-center gap-1.5 px-3.5 py-2 text-xs font-semibold text-white bg-indigo-600 hover:bg-indigo-700 rounded-lg shadow-sm transition"
            >
              <SlidersHorizontal className="w-3.5 h-3.5" />
              <span>Manual Adjustment</span>
            </button>
          </div>
        }
      />

      {/* KPI Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <StatCard
          title="Total Stock Units"
          value={isSummaryLoading ? '...' : formatNumber(summary?.totalStockQuantity || summary?.totalUnits || 0)}
          subtitle="Physical units across shelves"
          icon={Boxes}
          color="indigo"
        />
        <StatCard
          title="Total Inventory Value"
          value={isSummaryLoading ? '...' : formatCurrency(summary?.totalInventoryCostValue || summary?.inventoryValue || 0)}
          subtitle="Purchase cost of stock on hand"
          icon={IndianRupee}
          color="blue"
        />
        <StatCard
          title="Low-Stock Alerts"
          value={isSummaryLoading ? '...' : summary?.lowStockCount || 0}
          subtitle="Items below safety threshold"
          icon={AlertTriangle}
          color="amber"
        />
        <StatCard
          title="Out-of-Stock Items"
          value={isSummaryLoading ? '...' : summary?.outOfStockCount || 0}
          subtitle="Immediate restock needed"
          icon={PackageX}
          color="red"
        />
      </div>

      {/* Filters bar */}
      <div className="bg-white p-4 rounded-xl border border-slate-200 flex flex-col sm:flex-row sm:items-center justify-between gap-3 shadow-xs">
        <div className="flex flex-col sm:flex-row sm:items-center gap-3 flex-1">
          <SearchInput
            value={search}
            onChange={(val) => {
              setSearch(val);
              setCurrentPage(1);
            }}
            placeholder="Search inventory by product or SKU..."
          />
          <FilterDropdown
            label="All Stock Status"
            value={stockFilter}
            options={[
              { label: 'In Stock (> Min)', value: 'in' },
              { label: 'Low Stock (<= Min)', value: 'low' },
              { label: 'Out of Stock (0)', value: 'out' },
            ]}
            onChange={(val) => {
              setStockFilter(val);
              setCurrentPage(1);
            }}
          />
        </div>

        <div className="text-xs text-slate-500 font-medium">
          Showing <span className="font-semibold text-slate-900">{filteredProducts.length}</span> inventory items
        </div>
      </div>

      {/* Table */}
      <DataTable
        columns={columns}
        data={paginatedData}
        isLoading={isProductsLoading}
        onRowClick={(row) => navigate(`/products/${row.id}`)}
      />

      <Pagination
        currentPage={currentPage}
        totalPages={totalPages}
        onPageChange={setCurrentPage}
      />

      {/* Stock adjustment modal */}
      {isAdjustOpen && (
        <StockAdjustmentModal
          isOpen={isAdjustOpen}
          onClose={() => {
            setIsAdjustOpen(false);
            setAdjustProduct(null);
          }}
          initialProduct={adjustProduct}
          products={products}
        />
      )}
    </div>
  );
}

export default InventoryPage;
