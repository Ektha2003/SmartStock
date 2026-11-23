import React from 'react';
import { useQuery } from '@tanstack/react-query';
import {
  Package,
  IndianRupee,
  ShoppingBag,
  AlertTriangle,
  PackageX,
  Sparkles,
} from 'lucide-react';
import StatCard from '../../components/common/StatCard';
import PageHeader from '../../components/common/PageHeader';
import SalesOverviewChart from '../../components/dashboard/SalesOverviewChart';
import TopProductsCard from '../../components/dashboard/TopProductsCard';
import LowStockAlertsCard from '../../components/dashboard/LowStockAlertsCard';
import RecentTransactionsCard from '../../components/dashboard/RecentTransactionsCard';
import QuickActionsCard from '../../components/dashboard/QuickActionsCard';
import dashboardService from '../../services/dashboardService';
import productService from '../../services/productService';
import inventoryService from '../../services/inventoryService';
import { formatCurrency, formatNumber } from '../../utils/formatters';
import useLanguage from '../../hooks/useLanguage';
import { Link } from 'react-router-dom';

export function DashboardPage() {
  const { t } = useLanguage();

  const { data: summary, isLoading: isSummaryLoading } = useQuery({
    queryKey: ['dashboard', 'summary'],
    queryFn: () => dashboardService.getSummary(),
  });

  const { data: chartData, isLoading: isChartLoading } = useQuery({
    queryKey: ['dashboard', 'sales-chart'],
    queryFn: () => dashboardService.getSalesChart('daily', 30),
  });

  const { data: topProducts, isLoading: isTopProductsLoading } = useQuery({
    queryKey: ['dashboard', 'top-products'],
    queryFn: () => dashboardService.getTopProducts(5),
  });

  const { data: productsData, isLoading: isProductsLoading } = useQuery({
    queryKey: ['products'],
    queryFn: () => productService.getProducts({ pageSize: 50 }),
  });

  const { data: transactionsData, isLoading: isTransactionsLoading } = useQuery({
    queryKey: ['inventory', 'transactions'],
    queryFn: () => inventoryService.getTransactions({ pageSize: 10 }),
  });

  const products = productsData?.items || (Array.isArray(productsData) ? productsData : []);
  const transactions = transactionsData?.items || (Array.isArray(transactionsData) ? transactionsData : []);

  return (
    <div className="space-y-6">
      <PageHeader
        title={t('dashboard.title')}
        subtitle={t('dashboard.subtitle')}
        action={
          <Link
            to="/assistant"
            className="inline-flex items-center gap-2 px-3.5 py-2 bg-gradient-to-r from-indigo-600 to-indigo-700 hover:from-indigo-700 hover:to-indigo-800 text-white text-xs font-semibold rounded-lg shadow-sm transition"
          >
            <Sparkles className="w-4 h-4 text-amber-300" />
            <span>SmartStock AI Advice</span>
          </Link>
        }
      />

      {/* KPI Cards Grid */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-5 gap-4">
        <StatCard
          title={t('dashboard.totalProducts')}
          value={isSummaryLoading ? '...' : formatNumber(summary?.totalProducts || 0)}
          subtitle="Active catalog SKUs"
          icon={Package}
          color="indigo"
        />
        <StatCard
          title={t('dashboard.stockValue')}
          value={isSummaryLoading ? '...' : formatCurrency(summary?.totalInventoryValue || summary?.totalStockValue || 0)}
          subtitle="At wholesale purchase cost"
          icon={IndianRupee}
          color="blue"
        />
        <StatCard
          title={t('dashboard.todaysSales')}
          value={isSummaryLoading ? '...' : formatCurrency(summary?.totalSalesToday || summary?.todaysSales || 0)}
          subtitle="Today's total store revenue"
          trend="up"
          trendValue="Live POS"
          icon={ShoppingBag}
          color="green"
        />
        <StatCard
          title={t('dashboard.lowStock')}
          value={isSummaryLoading ? '...' : summary?.lowStockProductCount || summary?.lowStockItems || 0}
          subtitle="Below safety threshold"
          trend="down"
          trendValue="Needs order"
          icon={AlertTriangle}
          color="amber"
        />
        <StatCard
          title={t('dashboard.outOfStock')}
          value={isSummaryLoading ? '...' : summary?.outOfStockProductCount || summary?.outOfStockItems || 0}
          subtitle="Zero units on shelf"
          icon={PackageX}
          color="red"
        />
      </div>

      {/* Quick Action Buttons */}
      <QuickActionsCard />

      {/* Sales Overview and Top Selling Products */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div className="lg:col-span-2">
          <SalesOverviewChart data={chartData} isLoading={isChartLoading} />
        </div>
        <div>
          <TopProductsCard products={topProducts} isLoading={isTopProductsLoading} />
        </div>
      </div>

      {/* Low Stock Alerts and Recent Transactions */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <LowStockAlertsCard products={products} isLoading={isProductsLoading} />
        <RecentTransactionsCard transactions={transactions} isLoading={isTransactionsLoading} />
      </div>
    </div>
  );
}

export default DashboardPage;
