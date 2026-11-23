# SmartStock – AI-Powered Inventory & Business Management for Indian Retailers

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4.svg?style=flat&logo=dotnet)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-18.4-4169E1.svg?style=flat&logo=postgresql)](https://www.postgresql.org/)
[![EF Core](https://img.shields.io/badge/EF%20Core-10.0-blue.svg?style=flat)](https://learn.microsoft.com/en-us/ef/core/)
[![Gemini AI](https://img.shields.io/badge/Google-Gemini%20AI-orange.svg?style=flat&logo=google)](https://ai.google.dev/)
[![Tests](https://img.shields.io/badge/Tests-18%20Passed-brightgreen.svg?style=flat)](#testing)

SmartStock is a production-grade, multi-tenant inventory, sales, and predictive business management backend engineered specifically for Indian kirana stores, supermarkets, and small retailers. It combines multi-tenant relational isolation, ACID transactions with PostgreSQL execution strategies, demand forecasting, stock replenishment recommendations, and Google Gemini AI multimodal intelligence.

---

## Table of Contents

- [Architectural Overview](#architectural-overview)
- [Key Features](#key-features)
- [Tech Stack](#tech-stack)
- [Solution Structure](#solution-structure)
- [Database Schema & Multi-Tenancy](#database-schema--multi-tenancy)
- [Prerequisites](#prerequisites)
- [Configuration & Environment Variables](#configuration--environment-variables)
- [Database Migrations & Seeding](#database-migrations--seeding)
- [Demo Store & Credentials](#demo-store--credentials)
- [API Reference](#api-reference)
- [AI & Forecasting Capabilities](#ai--forecasting-capabilities)
- [Testing](#testing)
- [Running the Application](#running-the-application)

---

## Architectural Overview

SmartStock employs a **Clean Modular Monolith** architecture with strict separation of concerns, avoiding unnecessary CQRS or microservice complexity:

```
SmartStock/
└── backend/
    ├── SmartStock.sln
    ├── src/
    │   ├── SmartStock.Core/           # Domain Entities, Enums, DTOs, Interfaces, Exceptions, Validators
    │   ├── SmartStock.Infrastructure/ # EF Core DbContext, Repositories, AI Integration, Forecasting Engine, Seed Data
    │   └── SmartStock.API/            # Controllers, JWT Middleware, Exception Handler, Swagger, Program.cs
    └── tests/
        └── SmartStock.Tests/          # Unit Tests, Multi-tenant Isolation Tests, PostgreSQL Transaction Tests
```

### Core Architectural Principles
1. **Zero-Trust Multi-Tenancy**: The client is never trusted to supply `BusinessId`. The authenticated tenant is resolved exclusively from validated claims in the JWT token via `ICurrentUserService`.
2. **PostgreSQL Resilient Execution Strategy**: All database transactions (`Sales`, `Purchases`, `Inventory Adjustments`) run inside `_context.Database.CreateExecutionStrategy().ExecuteAsync(...)` to ensure full compatibility with Npgsql retry policies.
3. **Double-Audit Synchronization**: No stock quantity changes occur in isolation. Every stock change writes a mandatory `StockTransaction` record and updates `Product.CurrentStock` in an atomic ACID transaction.
4. **Graceful AI Degradation**: Gemini Assistant and Invoice OCR degrade gracefully to deterministic heuristic algorithms if the API key is not configured or upstream limits are reached.

---

## Key Features

- **Multi-Tenant Isolation**: Separate tenant records, categories, products, inventory history, sales, and analytics.
- **Stock Audit Trail**: Tracks `Opening`, `Purchase`, `Sale`, and `Adjustment` movements with previous/new stock tracking.
- **Atomic Sales & Invoicing**: Atomic transactions validate item availability, prevent negative stock, calculate line/total amounts on the server, update inventory, and log transactions.
- **Low-Stock Notifications**: Automated triggers when `CurrentStock <= MinimumStock` or `CurrentStock == 0` without spamming duplicate unread notifications.
- **Demand Forecasting Engine**: 7-day rolling window velocity, days-of-stock remaining, and safety stock risk levels (`Good`, `Moderate`, `Critical`, `OutOfStock`).
- **Smart Purchase Recommendations**: Dynamically calculates reorder quantities based on target 10-day buffer stock, unit purchase costs, and mapped suppliers.
- **Gemini AI Business Assistant**: Indian Kirana-tailored business advisor with natural language conversational capabilities, prompt-injection defense, and store-specific context injection.
- **Invoice OCR & Multi-Modal Scanner**: Scans vendor bills/invoices (JPEG/PNG/PDF) using Gemini Vision API to extract itemized product names, quantities, unit costs, and tax/invoice metadata.

---

## Tech Stack

| Technology | Purpose |
|------------|---------|
| **.NET 10 (C# 13)** | High-performance Web API platform |
| **ASP.NET Core Web API** | REST API endpoints, routing, dependency injection |
| **Entity Framework Core 10** | Object-Relational Mapping (ORM) and Fluent Configurations |
| **PostgreSQL 18.4 / Npgsql 10** | Relational database with full ACID compliance |
| **JWT Bearer Authentication** | Stateless token authentication with role & tenant claims |
| **BCrypt.Net-Next** | Secure cryptographic password hashing |
| **FluentValidation** | Request payload validation pipeline |
| **Google Gemini API** | Multimodal AI assistant and OCR bill scanning |
| **Swashbuckle (Swagger UI)** | Interactive OpenAPI documentation with Bearer Auth |
| **xUnit & FluentAssertions** | Comprehensive unit, isolation, and integration testing |

---

## Solution Structure

```
SmartStock/backend/
├── SmartStock.sln
├── .env.example
├── README.md
├── src/
│   ├── SmartStock.Core/
│   │   ├── Common/
│   │   │   └── CommonModels.cs       # ApiResponse<T>, PagedResult<T>, PaginationParams
│   │   ├── DTOs/
│   │   │   ├── AuthAndBusinessDtos.cs
│   │   │   ├── CatalogDtos.cs
│   │   │   ├── TransactionDtos.cs
│   │   │   └── DashboardAndAiDtos.cs
│   │   ├── Entities/
│   │   │   ├── BaseEntity.cs
│   │   │   ├── BusinessAndUser.cs    # Business, User, RefreshToken
│   │   │   ├── Catalog.cs            # Category, Product, Supplier
│   │   │   └── Transactions.cs       # Sale, SaleItem, Purchase, PurchaseItem, StockTransaction, Notification
│   │   ├── Enums/
│   │   │   └── Enums.cs              # UserRole, UnitType, TransactionType, NotificationType, StockRiskLevel
│   │   ├── Exceptions/
│   │   │   └── Exceptions.cs         # AppException, NotFoundException, InsufficientStockException, etc.
│   │   ├── Interfaces/
│   │   │   └── IServices.cs          # Interfaces for all business, auth, sales, AI services
│   │   └── Validation/
│   │       └── Validators.cs         # FluentValidation rules
│   │
│   ├── SmartStock.Infrastructure/
│   │   ├── AI/
│   │   │   ├── AIAssistantService.cs # Gemini Chat integration + Fallback
│   │   │   └── InvoiceScannerService.cs # Gemini Multimodal bill extraction
│   │   ├── Data/
│   │   │   ├── ApplicationDbContext.cs
│   │   │   ├── IApplicationDbContext.cs
│   │   │   ├── Configurations/
│   │   │   │   └── EntityConfigurations.cs # Fluent API mappings & constraints
│   │   │   ├── Migrations/           # EF Core migration files
│   │   │   └── Seed/
│   │   │       └── DatabaseSeeder.cs # 36 Indian products, 30 days sales history
│   │   ├── Forecasting/
│   │   │   └── ForecastingService.cs # Velocity calculations & Purchase suggestions
│   │   └── Services/
│   │       ├── AuthService.cs
│   │       ├── BusinessService.cs
│   │       ├── CategoryService.cs
│   │       ├── CurrentUserService.cs
│   │       ├── DashboardService.cs
│   │       ├── InventoryService.cs
│   │       ├── NotificationService.cs
│   │       ├── ProductService.cs
│   │       ├── PurchaseService.cs
│   │       ├── SalesService.cs
│   │       └── SupplierService.cs
│   │
│   └── SmartStock.API/
│       ├── Controllers/              # 13 REST API Controllers
│       ├── Middleware/
│       │   └── ExceptionHandlingMiddleware.cs
│       ├── appsettings.json
│       ├── appsettings.Development.json
│       └── Program.cs
│
└── tests/
    └── SmartStock.Tests/
        ├── AuthServiceTests.cs
        ├── BusinessIsolationTests.cs
        ├── ForecastingAndRecommendationTests.cs
        ├── PostgresTransactionTests.cs
        ├── ProductAndValidationTests.cs
        ├── SalesAndPurchaseStockTests.cs
        └── TestDbContextFactory.cs
```

---

## Database Schema & Multi-Tenancy

Every domain entity derives from `BaseEntity` (providing `Id`, `CreatedAt`, `UpdatedAt`) and carries a `BusinessId` foreign key.

### Entity Relationships & Constraints
- **Multi-Tenant SKU Uniqueness**: Unique index on `(BusinessId, SKU)`.
- **Multi-Tenant Category Uniqueness**: Unique index on `(BusinessId, Name)`.
- **High-Performance Query Indexes**: Indexed on `(BusinessId, CreatedAt)`, `(BusinessId, ProductId)`, `(BusinessId, IsActive)`.
- **Decimal Precision**:
  - Currency (`PurchasePrice`, `SellingPrice`, `UnitPrice`, `TotalAmount`): `numeric(18,2)`
  - Fractional Quantities (`CurrentStock`, `MinimumStock`, `Quantity`): `numeric(18,3)` to support fractional kilograms, litres, and grams.
- **Referential Integrity**: DeleteBehavior set to `Restrict` on critical history (`Sales`, `Purchases`, `StockTransactions`) to prevent accidental data loss.

---

## Prerequisites

1. [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
2. [PostgreSQL 14+](https://www.postgresql.org/download/) running on port `5432`
3. Optional: [Google Gemini API Key](https://aistudio.google.com/) for live AI assistant & vision invoice extraction

---

## Configuration & Environment Variables

Copy `.env.example` to `.env` or configure `appsettings.json` / system environment variables:

```bash
cp .env.example .env
```

### Available Settings

| Key | Description | Default |
|-----|-------------|---------|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string | `Host=localhost;Port=5432;Database=smartstock_db;Username=postgres;Password=...` |
| `Jwt__Secret` | Signing key (min 32 chars) | `SmartStockSuperSecureJwtSigningKeyForHackathon2026WithSufficientLength!` |
| `Jwt__Issuer` | JWT Token Issuer | `SmartStockAPI` |
| `Jwt__Audience` | JWT Token Audience | `SmartStockClient` |
| `Jwt__AccessTokenExpirationMinutes` | Access Token Lifetime | `1440` (24 hours) |
| `Jwt__RefreshTokenExpirationDays` | Refresh Token Lifetime | `7` |
| `Gemini__ApiKey` | Google AI Studio Gemini API Key | *(Optional)* |
| `Gemini__Model` | Gemini AI model identifier | `gemini-2.5-flash` |
| `Frontend__Origin` | Allowed CORS origins (semicolon-delimited) | `http://localhost:5173;http://localhost:3000` |

---

## Database Migrations & Seeding

The application automatically checks for pending migrations and applies them on startup. It also automatically seeds the demo store if the database is empty.

### Applying Migrations Manually via CLI

```bash
# Navigate to backend directory
cd SmartStock/backend

# Apply migration to database
dotnet ef database update --project src/SmartStock.Infrastructure --startup-project src/SmartStock.API
```

### Adding New Migrations

```bash
dotnet ef migrations add <MigrationName> --project src/SmartStock.Infrastructure --startup-project src/SmartStock.API --output-dir Data/Migrations
```

---

## Demo Store & Credentials

A realistic demo store is automatically seeded upon initial startup:

- **Business Name**: `Sharma Kirana & General Store`
- **Owner Name**: `Ramesh Sharma`
- **Email**: `demo@smartstock.in`
- **Password**: `Password123!`
- **Role**: `Owner`

### Seeded Inventory Highlights
- **6 Categories**: Grains & Staples, Dairy & Breakfast, Snacks & Beverages, Personal Care & Hygiene, Spices & Condiments, Household & Cleaning.
- **36 Realistic Indian Retail Products**: Aashirvaad Shudh Chakki Atta, India Gate Basmati Rice, Fortune Sunflower Oil, Tata Salt, Amul Butter, Amul Taaza Milk, Maggi 2-Minute Noodles, Parle-G Biscuits, Dettol Antiseptic, Haldiram's Bhujia, and more.
- **Suppliers**: Delhi Wholesale FMCG Traders, Amul Dairy Depot North, Hindustan Unilever Distributors.
- **Stock States**:
  - High stock staples (Atta, Rice, Oil)
  - Low stock items triggering alerts (`Tata Salt`, `Amul Butter`, `Maggi Noodles`)
  - Out of stock item (`Amul Taaza Milk`)
- **Historical Sales**: 30 days of multi-item realistic customer transactions with daily variations.

---

## API Reference

All authenticated endpoints require the `Authorization: Bearer <token>` header.

### 1. Authentication (`/api/v1/auth`)
| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| `POST` | `/api/v1/auth/register` | Register new business and owner user | No |
| `POST` | `/api/v1/auth/login` | Authenticate and obtain JWT + Refresh token | No |
| `POST` | `/api/v1/auth/refresh` | Refresh expired access token | No |
| `GET` | `/api/v1/auth/me` | Get current user profile and business info | Yes |

### 2. Business Management (`/api/v1/business`)
| Method | Endpoint | Description | Role |
|--------|----------|-------------|------|
| `GET` | `/api/v1/business` | Get authenticated business details | Any |
| `PUT` | `/api/v1/business` | Update business profile details | Owner |

### 3. Categories (`/api/v1/categories`)
| Method | Endpoint | Description | Role |
|--------|----------|-------------|------|
| `GET` | `/api/v1/categories` | List all categories with product counts | Any |
| `GET` | `/api/v1/categories/{id}` | Get category details | Any |
| `POST` | `/api/v1/categories` | Create new category | Owner |
| `PUT` | `/api/v1/categories/{id}` | Update category | Owner |
| `DELETE`| `/api/v1/categories/{id}` | Delete category (blocked if active products exist)| Owner |

### 4. Products (`/api/v1/products`)
| Method | Endpoint | Description | Query Parameters |
|--------|----------|-------------|------------------|
| `GET` | `/api/v1/products` | Paginated product search & catalog | `searchTerm`, `categoryId`, `isActive`, `lowStockOnly`, `page`, `pageSize`, `sortBy` |
| `GET` | `/api/v1/products/{id}` | Get detailed product by ID | - |
| `GET` | `/api/v1/products/low-stock` | Get low-stock and out-of-stock items | - |
| `POST` | `/api/v1/products` | Create product (auto-generates Opening Stock) | - |
| `PUT` | `/api/v1/products/{id}` | Update product details | - |
| `DELETE`| `/api/v1/products/{id}` | Soft-delete product (sets `IsActive = false`) | - |

### 5. Suppliers (`/api/v1/suppliers`)
| Method | Endpoint | Description | Query Parameters |
|--------|----------|-------------|------------------|
| `GET` | `/api/v1/suppliers` | Paginated supplier directory | `searchTerm`, `page`, `pageSize` |
| `GET` | `/api/v1/suppliers/{id}` | Get supplier by ID | - |
| `POST` | `/api/v1/suppliers` | Create supplier | - |
| `PUT` | `/api/v1/suppliers/{id}` | Update supplier | - |
| `DELETE`| `/api/v1/suppliers/{id}` | Soft-delete supplier | - |

### 6. Sales & Billing (`/api/v1/sales`)
| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/api/v1/sales` | Create multi-item sale (atomic stock deduction & invoice generation) |
| `GET` | `/api/v1/sales` | Paginated sales transactions (`page`, `pageSize`, `startDate`, `endDate`) |
| `GET` | `/api/v1/sales/{id}` | Get sale by ID with itemized lines |
| `GET` | `/api/v1/sales/summary` | Aggregated sales metrics (daily, weekly, monthly) |

#### Example Sale Request (`POST /api/v1/sales`)
```json
{
  "saleDate": "2026-11-22T10:00:00Z",
  "notes": "Walk-in cash customer",
  "items": [
    {
      "productId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "quantity": 2,
      "unitPrice": 450.00
    }
  ]
}
```

### 7. Purchases & Stock Inward (`/api/v1/purchases`)
| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/api/v1/purchases` | Record stock purchase from supplier (atomic inventory increment) |
| `GET` | `/api/v1/purchases` | Paginated purchase records |
| `GET` | `/api/v1/purchases/{id}` | Get purchase details with line items |
| `GET` | `/api/v1/purchases/summary` | Summary of purchase spend |

### 8. Inventory & Auditing (`/api/v1/inventory`)
| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/v1/inventory/summary` | Summary of stock valuation and counts |
| `GET` | `/api/v1/inventory/transactions` | Paginated audit trail of stock movements |
| `GET` | `/api/v1/inventory/product/{productId}/history` | Historical audit trail for a specific product |
| `POST` | `/api/v1/inventory/adjustment` | Manual audited stock adjustment (`Damage`, `Theft`, `Audit`) |

### 9. Dashboard & Business Analytics (`/api/v1/dashboard`)
| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/v1/dashboard/summary` | Complete KPI overview (sales today/month, stock counts, valuation) |
| `GET` | `/api/v1/dashboard/sales-chart` | Daily / monthly sales chart data (`period=daily` or `monthly`) |
| `GET` | `/api/v1/dashboard/top-products` | Top-selling products by quantity and revenue (`count=5`) |
| `GET` | `/api/v1/dashboard/stock-value` | Detailed inventory valuation grouped by category |

### 10. Low-Stock Notifications (`/api/v1/notifications`)
| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/v1/notifications` | List unread or all stock notifications (`unreadOnly=true`) |
| `PATCH`| `/api/v1/notifications/{id}/read` | Mark individual notification as read |
| `PATCH`| `/api/v1/notifications/read-all` | Mark all business notifications as read |
| `POST` | `/api/v1/notifications/check-stock` | Trigger automated stock scan and alert generation |

### 11. Demand Forecasting (`/api/v1/forecast`)
| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/v1/forecast` | Full catalogue demand forecast and risk classification |
| `GET` | `/api/v1/forecast/product/{productId}` | Demand forecast for a single product |

### 12. Smart Purchase Recommendations (`/api/v1/recommendations`)
| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/v1/recommendations/purchases` | Suggested purchase orders with buffer targets and estimated costs |

### 13. AI Business Assistant & Invoice Scanner (`/api/v1/ai`)
| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/api/v1/ai/assistant` | Conversational query to Gemini AI with store-context injection |
| `POST` | `/api/v1/ai/scan-invoice` | Multimodal OCR invoice scanning (`multipart/form-data`) |

---

## AI & Forecasting Capabilities

### 1. Demand Forecasting Engine
- Calculates 7-day sales velocity:
  $$\text{AvgDailySales} = \frac{\sum \text{Quantity Sold (Last 7 Days)}}{7}$$
- Computes runway:
  $$\text{DaysRemaining} = \frac{\text{CurrentStock}}{\text{AvgDailySales}}$$
- Automatically categorizes risk levels:
  - **`OutOfStock`**: CurrentStock == 0
  - **`Critical`**: DaysRemaining $\le$ 2
  - **`Moderate`**: DaysRemaining $\le$ 5
  - **`Good`**: DaysRemaining > 5

### 2. Purchase Recommendations
- Determines ideal reorder buffer based on 10 days of forward demand:
  $$\text{TargetStock} = \lceil\text{AvgDailySales} \times 10\rceil$$
  $$\text{SuggestedQuantity} = \max(0, \text{TargetStock} - \text{CurrentStock})$$
- Computes estimated procurement costs and maps default suppliers.

### 3. Conversational AI Assistant (Google Gemini)
- Prompts are enriched with real-time store metrics (today's sales, low stock counts, critical shortage names).
- Context-sanitized against prompt-injection vulnerabilities.
- Automatic fallback delivers rule-based insights if internet or API keys are unavailable.

### 4. Vision Invoice Scanner
- Accepts images (`image/jpeg`, `image/png`) or PDFs of supplier paper bills.
- Employs Gemini Multimodal Vision to extract item names, quantities, unit prices, line totals, and supplier details.
- Returns clean JSON ready for 1-click conversion into an inward `Purchase` record.

---

## Testing

SmartStock includes 18 unit, business logic, multi-tenant isolation, and PostgreSQL ACID transaction tests.

### Running the Test Suite

```bash
cd SmartStock/backend
dotnet test
```

### Test Coverage Highlights
- `AuthServiceTests.cs`: Password hashing, JWT token validation, refresh tokens, role claims.
- `BusinessIsolationTests.cs`: Verifies that tenants cannot read, modify, or delete another business's products or categories.
- `ProductAndValidationTests.cs`: SKU uniqueness per business, positive price validation, soft deletion.
- `SalesAndPurchaseStockTests.cs`: Inventory reduction on sales, stock increase on purchases, negative stock prevention, and audit transaction logging.
- `ForecastingAndRecommendationTests.cs`: Rolling daily sales velocity, zero-sales handling, safety stock calculations.
- `PostgresTransactionTests.cs`: Verifies PostgreSQL ACID rollback behavior using execution strategies when errors occur.

---

## Running the Application

### 1. Start PostgreSQL
Ensure PostgreSQL is running on `localhost:5432` with the `smartstock_db` database created:

```bash
# Example PostgreSQL commands
psql -U postgres -c "CREATE DATABASE smartstock_db;"
```

### 2. Launch the Web API
```bash
cd SmartStock/backend/src/SmartStock.API
dotnet run
```

The application will start on:
- API Base URL: `http://localhost:5000`
- Interactive Swagger UI: `http://localhost:5000/swagger`
- Health Check: `http://localhost:5000/api/health`

### 3. Log In to the Swagger UI
1. Open `http://localhost:5000/swagger`.
2. Execute `POST /api/v1/auth/login` with:
   ```json
   {
     "email": "demo@smartstock.in",
     "password": "Password123!"
   }
   ```
3. Copy the returned `accessToken`.
4. Click the **Authorize** button at the top right of the Swagger UI and enter:
   ```
   Bearer <your_access_token>
   ```
5. All endpoints are now unlocked for testing.
