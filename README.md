# SmartStock – AI-Powered Inventory & Retail Automation

> **Hack With Delhi 4.0 Finale Project**  
> Built for Indian Kirana Stores, Supermarkets & Small Retailers.  
> **Powered by CovexCode LLM (`deepseek-v4-flash`)** 🚀

[![Hack With Delhi 4.0](https://img.shields.io/badge/Hackathon-Hack%20With%20Delhi%204.0-ff0055.svg?style=for-the-badge)](https://covexcode.com/)
[![Powered by CovexCode](https://img.shields.io/badge/AI%20Engine-CovexCode%20DeepSeek--v4--Flash-00C853.svg?style=for-the-badge)](https://covexcode.com/)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4.svg?style=for-the-badge&logo=dotnet)](https://dotnet.microsoft.com/)
[![React](https://img.shields.io/badge/React-18-61DAFB.svg?style=for-the-badge&logo=react)](https://react.dev/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-18-4169E1.svg?style=for-the-badge&logo=postgresql)](https://www.postgresql.org/)

---

## 📌 Executive Summary

Small retail and grocery (Kirana) store owners face three critical operational bottlenecks every day:
1. **Manual Bill Entry**: Typing lengthy paper invoices from suppliers takes hours and leads to expensive data-entry errors.
2. **Hidden Wholesale Price Hikes**: Distributors quietly increase unit prices; shopkeepers don't notice and lose their retail margins.
3. **Stockouts & Dead Inventory**: Owners guess when to restock, leading to running out of essential groceries on weekends or tying up cash in slow-moving stock.

**SmartStock** transforms store operations with an autonomous AI copilot that reads paper invoices, flags price increases, predicts stockout dates, drafts customer delivery reminders, and processes natural language email replies.

---

## ⚡ Powered by CovexCode AI

SmartStock natively integrates **CovexCode's LLM (`deepseek-v4-flash`)** as its primary artificial intelligence engine:

| Capability | How CovexCode AI Powers It | Application Tab |
| :--- | :--- | :--- |
| **Store Copilot & Advisor** | Answers business and restocking questions in plain conversational language or Hindi using live store metrics. | **AI Assistant** (`/assistant`) |
| **Customer Email Generator** | Generates warm, personalized upcoming delivery emails for regular subscribers with 1-click approvals. | **Recurring Orders** (`/recurring-orders`) |
| **Email Intent & Order Processor** | Analyzes customer reply emails (e.g., *"Skip milk this week and add 1kg sugar"*) to extract intent and auto-modify pending orders. | **Recurring Orders** (`/recurring-orders`) |
| **Invoice Scanner & Price Guardian** | Multimodal OCR extracts printed and handwritten line items and automatically flags wholesale price hikes. | **AI Invoices** (`/supplier-invoices`) |

---

## 🧪 Testing CovexCode Live (For Judges & Team Members)

You can verify the **CovexCode (`deepseek-v4-flash`)** integration and record live token usage in under 10 seconds:

```bash
# Run with your CovexCode API Key:
python scripts/test_covexcode.py bx-live-YOUR_API_KEY_HERE
```

Or set the environment variable:
```bash
export COVEXCODE_ENDPOINT=https://llm.covexcode.com/v1
export COVEXCODE_MODEL=deepseek-v4-flash
export COVEXCODE_API_KEY=bx-live-YOUR_API_KEY_HERE

python scripts/test_covexcode.py
```

Expected output:
```text
[*] Testing SmartStock AI with CovexCode...
    - Endpoint : https://llm.covexcode.com/v1
    - Model    : deepseek-v4-flash
    - Key      : bx-live-...

[+] SUCCESS! Response received from CovexCode
[+] Total Tokens Consumed: 208
[+] Your CovexCode token usage has been recorded on the backend!
```

---

## 🏗️ System Architecture & Structure

```
SmartStock/
├── backend/                        # ASP.NET Core Web API (.NET 10)
│   ├── src/
│   │   ├── SmartStock.Core/        # Domain entities, DTOs, interfaces, business rules
│   │   ├── SmartStock.Infrastructure/ # CovexCode AI integration, EF Core, Repositories
│   │   └── SmartStock.API/         # Controllers, JWT Auth, Swagger, appsettings.json
│   └── tests/SmartStock.Tests/     # Automated unit & integration test suite
│
├── frontend/                       # Modern React 18 + Tailwind CSS + Lucide Icons
│   ├── src/
│   │   ├── components/             # Reusable UI widgets, drawers, modals, charts
│   │   ├── pages/                  # Dashboard, Inventory, Invoices, Insights, Assistant
│   │   └── services/               # API clients & state management
│
└── scripts/                        # Utility & testing scripts
    └── test_covexcode.py           # Live CovexCode AI verification script
```

---

## 🚀 Quick Start Guide

### 1. Prerequisites
- **.NET 10 SDK**
- **Node.js 18+** & npm
- **PostgreSQL 14+**
- **Python 3.8+** (for test script)

### 2. Configure Backend (`backend/src/SmartStock.API/appsettings.json`)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=smartstock_db;Username=postgres;Password=YourPassword"
  },
  "CovexCode": {
    "Endpoint": "https://llm.covexcode.com/v1",
    "Model": "deepseek-v4-flash",
    "ApiKey": "bx-live-YOUR_KEY_HERE"
  }
}
```

### 3. Run Backend
```bash
cd backend/src/SmartStock.API
dotnet run
# API runs on https://localhost:7001 (Swagger available at /swagger)
```

### 4. Run Frontend
```bash
cd frontend
npm install
npm run dev
# Web application opens on http://localhost:5173
```

---

## 👥 Team & Submission Info

- **Hackathon**: Hack With Delhi 4.0
- **AI Platform**: [CovexCode](https://covexcode.com/)
- **Project**: SmartStock Retail Automation
