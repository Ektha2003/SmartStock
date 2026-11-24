import React from 'react';
import { Routes, Route, Navigate } from 'react-router-dom';
import ProtectedRoute, { PublicRoute } from './ProtectedRoute';
import AppLayout from '../layouts/AppLayout';
import AuthLayout from '../layouts/AuthLayout';

// Auth Pages
import LoginPage from '../pages/auth/LoginPage';
import RegisterPage from '../pages/auth/RegisterPage';

// Main Protected Pages
import DashboardPage from '../pages/dashboard/DashboardPage';
import ProductsPage from '../pages/products/ProductsPage';
import NewProductPage from '../pages/products/NewProductPage';
import ProductDetailPage from '../pages/products/ProductDetailPage';
import CategoriesPage from '../pages/categories/CategoriesPage';
import InventoryPage from '../pages/inventory/InventoryPage';
import StockTransactionsPage from '../pages/inventory/StockTransactionsPage';
import SalesPage from '../pages/sales/SalesPage';
import NewSalePage from '../pages/sales/NewSalePage';
import SaleDetailPage from '../pages/sales/SaleDetailPage';
import PurchasesPage from '../pages/purchases/PurchasesPage';
import NewPurchasePage from '../pages/purchases/NewPurchasePage';
import PurchaseDetailPage from '../pages/purchases/PurchaseDetailPage';
// Suppliers & AI Invoice Automation
import SuppliersPage from '../pages/suppliers/SuppliersPage';
import SupplierDetailPage from '../pages/suppliers/SupplierDetailPage';
import SupplierInvoicesPage from '../pages/invoices/SupplierInvoicesPage';
import InvoiceReviewPage from '../pages/invoices/InvoiceReviewPage';
import NotificationsPage from '../pages/notifications/NotificationsPage';
import InsightsPage from '../pages/insights/InsightsPage';
import AssistantPage from '../pages/assistant/AssistantPage';
import SettingsPage from '../pages/settings/SettingsPage';
import NotFoundPage from '../pages/NotFoundPage';

// Customer & AI Recurring Order Management
import CustomersPage from '../pages/customers/CustomersPage';
import CustomerDetailPage from '../pages/customers/CustomerDetailPage';
import RecurringOrdersPage from '../pages/recurring/RecurringOrdersPage';
import OrderApprovalPublicPage from '../pages/recurring/OrderApprovalPublicPage';

export function AppRoutes() {
  return (
    <Routes>
      {/* Root redirect */}
      <Route path="/" element={<Navigate to="/dashboard" replace />} />

      {/* Public Customer Approval Portal (No login required) */}
      <Route path="/order-approval/:token" element={<OrderApprovalPublicPage />} />

      {/* Public Auth routes */}
      <Route
        element={
          <PublicRoute>
            <AuthLayout />
          </PublicRoute>
        }
      >
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<RegisterPage />} />
      </Route>

      {/* Protected SaaS application routes */}
      <Route
        element={
          <ProtectedRoute>
            <AppLayout />
          </ProtectedRoute>
        }
      >
        <Route path="/dashboard" element={<DashboardPage />} />
        
        {/* Products */}
        <Route path="/products" element={<ProductsPage />} />
        <Route path="/products/new" element={<NewProductPage />} />
        <Route path="/products/:id" element={<ProductDetailPage />} />

        {/* Categories */}
        <Route path="/categories" element={<CategoriesPage />} />

        {/* Inventory */}
        <Route path="/inventory" element={<InventoryPage />} />
        <Route path="/inventory/transactions" element={<StockTransactionsPage />} />

        {/* Sales */}
        <Route path="/sales" element={<SalesPage />} />
        <Route path="/sales/new" element={<NewSalePage />} />
        <Route path="/sales/:id" element={<SaleDetailPage />} />

        {/* Customers & AI Recurring Orders */}
        <Route path="/customers" element={<CustomersPage />} />
        <Route path="/customers/:id" element={<CustomerDetailPage />} />
        <Route path="/recurring-orders" element={<RecurringOrdersPage />} />

        {/* Purchases */}
        <Route path="/purchases" element={<PurchasesPage />} />
        <Route path="/purchases/new" element={<NewPurchasePage />} />
        <Route path="/purchases/:id" element={<PurchaseDetailPage />} />

        {/* Suppliers & AI Invoices */}
        <Route path="/suppliers" element={<SuppliersPage />} />
        <Route path="/suppliers/:id" element={<SupplierDetailPage />} />
        <Route path="/supplier-invoices" element={<SupplierInvoicesPage />} />
        <Route path="/supplier-invoices/:id/review" element={<InvoiceReviewPage />} />

        {/* Intelligence & Alerts */}
        <Route path="/notifications" element={<NotificationsPage />} />
        <Route path="/insights" element={<InsightsPage />} />
        <Route path="/assistant" element={<AssistantPage />} />

        {/* Settings */}
        <Route path="/settings" element={<SettingsPage />} />
      </Route>

      {/* 404 Route */}
      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  );
}

export default AppRoutes;

