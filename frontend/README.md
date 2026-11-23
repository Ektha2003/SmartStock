# SmartStock – AI-Powered Inventory & Business Assistant

SmartStock is a modern, commercial-grade SaaS frontend designed specifically for Indian Kirana store owners and small retail businesses. It streamlines daily retail workflows including Point-of-Sale (POS) counter billing, wholesale purchase receiving, live stock auditing, supplier management, and AI-driven predictive demand forecasting.

---

## 🌟 Key Features

1. **Dashboard Overview**: Real-time store KPIs (Total SKUs, Stock Valuation in ₹, Today’s Sales, Low Stock & Out-of-Stock count), weekly sales velocity chart (Recharts), top-selling basket contributors, and immediate quick actions.
2. **Products Catalog Management**: Full CRUD on product SKUs, wholesale cost vs retail pricing, category classification, safety stock thresholds, and stock status indicators.
3. **Categories Management**: Organize groceries into aisle departments (Staples & Grains, Edible Oils, Dairy, Snacks, Beverages, Household).
4. **Inventory & Stock Audit**: Live stock balances, physical stock reconciliation / adjustment modal with audit reasoning (Spoilage, Damage, Return, Reconciliation), and complete chronological stock movement ledger.
5. **Point of Sale (POS) Billing (`/sales/new`)**: Fast counter billing, real-time stock availability validation, customer phone tracking, multi-mode payment (UPI, Cash, Card, Udhaar), automatic stock decrement, and printable tax receipts.
6. **Wholesale Purchases (`/purchases/new`)**: Replenish stock directly from suppliers, multi-item bill entry, automatic inventory increment, and ledger tracking.
7. **Supplier Directory**: Contact registry for Mandi wholesalers, grain depots, and FMCG distributors.
8. **Stock Alert Notification Center**: Instant critical warnings when grocery items cross below safety levels.
9. **AI Demand Forecasting & Insights (`/insights`)**: 14-day rolling demand burn rate, stockout runway estimation in days, and a 1-click **"Create Purchase from Recommendations"** workflow.
10. **SmartStock AI Assistant (`/assistant`)**: Natural language chat assistant that analyzes your store's live database to answer questions about reordering, top sellers, inventory valuation, and dead stock.
11. **Bilingual Foundation (English & हिंदी)**: One-click header switcher for shopkeepers who prefer Hindi navigation and labels.
12. **Production-Ready & Dual-Mode Backend Support**:
    - **Standalone / Demo Mode (`VITE_USE_MOCK=true`)**: Instant offline demo with realistic Indian Kirana dataset stored reactively in `localStorage`.
    - **Live REST API Mode (`VITE_USE_MOCK=false`)**: Full integration with the ASP.NET Core backend at `VITE_API_BASE_URL`.

---

## 🚀 Getting Started

### 1. Prerequisites
- Node.js (v18+)
- npm (v9+)

### 2. Installation
Navigate into the `frontend` folder:
```bash
cd frontend
npm install
```

### 3. Environment Variables
Copy `.env.example` to `.env`:
```bash
cp .env.example .env
```

Configuration keys:
- `VITE_API_BASE_URL`: Base URL of the backend (Default: `http://localhost:5000/api/v1`)
- `VITE_USE_MOCK`: Set to `true` for standalone demo/hackathon judging without backend dependencies; set to `false` when connecting to live ASP.NET Core API.

### 4. Running the Development Server
```bash
npm run dev
```
Open your browser at `http://localhost:3000`.

### 5. Demo Login Credentials
Click the **"Use Demo Kirana Credentials (1-Click)"** button on the login screen, or enter:
- **Email**: `demo@smartstock.in`
- **Password**: `demo1234`

---

## 🛠 Tech Stack
- **Framework**: React 18 + Vite
- **Styling**: Tailwind CSS + Shadcn-style components
- **Routing**: React Router DOM v6
- **Server State**: TanStack Query (React Query v5)
- **Forms & Validation**: React Hook Form + Zod
- **Icons**: Lucide React
- **Charts**: Recharts
- **HTTP Client**: Axios with centralized interceptors
- **Notifications**: Sonner

---

## 📦 Building for Production
```bash
npm run build
```
This builds optimized static assets into the `frontend/dist/` directory.

