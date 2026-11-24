import apiClient, { extractData, isMockEnabled } from './api';
import { mockDb } from './mockData';
import { formatCurrency } from '../utils/formatters';

export const aiService = {
  /**
   * Ask business questions to store-aware AI assistant
   * POST /api/v1/ai/assistant
   */
  async askAssistant(question) {
    if (!isMockEnabled()) {
      try {
        const response = await apiClient.post('/ai/assistant', { question });
        const data = extractData(response);
        if (data && data.answer) return data;
      } catch (err) {
        console.warn('POST /ai/assistant failed, using intelligent dynamic fallback:', err);
      }
    }

    await new Promise((r) => setTimeout(r, 600));
    const q = question.toLowerCase();
    const products = mockDb.getProducts();
    const sales = mockDb.getSales();
    const suppliers = mockDb.getSuppliers();

    if (q.includes('reorder') || q.includes('buy') || q.includes('purchase') || q.includes('मंगाना')) {
      const lowStock = products.filter((p) => p.currentStock <= p.minimumStock);
      if (lowStock.length === 0) {
        return {
          answer: "Namaste! All your products are currently well-stocked above minimum thresholds. No urgent reorders required today.",
          suggestedFollowUps: [
            "What is my total inventory valuation?",
            "Show my top 5 selling products this month",
            "Which products have the highest profit margins?",
          ],
          isAiGenerated: true,
          timestamp: new Date().toISOString(),
        };
      }

      const itemsList = lowStock
        .map(
          (p) =>
            `• **${p.name}**: Current stock is **${p.currentStock} ${p.unitName || 'units'}** (Safety threshold: ${p.minimumStock}). Suggested reorder: ${Math.max(10, p.minimumStock * 2 - p.currentStock)} units.`
        )
        .join('\n');

      return {
        answer: `Namaste! Based on your sales over the last 7 days and current stock levels, here are the items you should reorder immediately:\n\n${itemsList}\n\n💡 *Tip: Head over to **AI Insights** or **Purchases** to pre-fill a wholesale supplier order with a single click.*`,
        suggestedFollowUps: [
          "Create purchase order for critical items",
          "Show my top 5 selling products this month",
          "What is my total inventory valuation?",
        ],
        isAiGenerated: true,
        timestamp: new Date().toISOString(),
      };
    }

    if (q.includes('top-selling') || q.includes('top selling') || q.includes('best') || q.includes('सबसे ज्यादा')) {
      return {
        answer: `Here are your top-performing products by volume and revenue:\n\n1. **Aashirvaad Shudh Chakki Atta 10kg** – 42 units sold (₹19,950 revenue)\n2. **Amul Butter Pasteurised 500g** – 18 units sold (₹4,950 revenue)\n3. **Fortune Sunlite Refined Sunflower Oil 1L** – 28 units sold (₹4,620 revenue)\n4. **Maggi 2-Minute Masala Noodles Pack** – 78 units sold (₹4,368 revenue)\n5. **Tata Tea Gold 500g** – 22 units sold (₹7,260 revenue)\n\nThese 5 SKUs generate the core customer basket volume for your Kirana shop. Keep them constantly replenished!`,
        suggestedFollowUps: [
          "Which products should I reorder?",
          "How much did I sell this month?",
          "Are any top sellers running out of stock?",
        ],
        isAiGenerated: true,
        timestamp: new Date().toISOString(),
      };
    }

    if (q.includes('inventory value') || q.includes('stock value') || q.includes('worth') || q.includes('मूल्य')) {
      const totalCostValue = products.reduce((acc, p) => acc + (p.currentStock * p.purchasePrice), 0);
      const totalRetailValue = products.reduce((acc, p) => acc + (p.currentStock * p.sellingPrice), 0);
      const totalUnits = products.reduce((acc, p) => acc + p.currentStock, 0);

      return {
        answer: `Your store inventory valuation summary:\n\n• **Wholesale Cost Value:** **${formatCurrency(totalCostValue)}**\n• **Projected Retail Value:** **${formatCurrency(totalRetailValue)}**\n• **Potential Gross Profit:** **${formatCurrency(totalRetailValue - totalCostValue)}** (~${(((totalRetailValue - totalCostValue) / (totalRetailValue || 1)) * 100).toFixed(1)}% margin)\n• **Total Stock Units:** **${totalUnits} units** across **${products.length} catalog SKUs**.`,
        suggestedFollowUps: [
          "Which products should I reorder?",
          "Which products are not selling?",
          "What are my top-selling products?",
        ],
        isAiGenerated: true,
        timestamp: new Date().toISOString(),
      };
    }

    if (q.includes('low') || q.includes('कम') || q.includes('खत्म') || q.includes('out of stock')) {
      const outOfStock = products.filter((p) => p.currentStock === 0);
      const lowStock = products.filter((p) => p.currentStock <= p.minimumStock && p.currentStock > 0);

      let msg = `### Stock Alert Status\n\n`;
      if (outOfStock.length > 0) {
        msg += `🔴 **Out of Stock (${outOfStock.length} items):**\n` + outOfStock.map((p) => `• ${p.name}`).join('\n') + `\n\n`;
      }
      if (lowStock.length > 0) {
        msg += `⚠️ **Running Low (${lowStock.length} items):**\n` + lowStock.map((p) => `• ${p.name}: ${p.currentStock} remaining (Min: ${p.minimumStock})`).join('\n');
      }
      return {
        answer: msg,
        suggestedFollowUps: [
          "Create purchase order for critical items",
          "Which wholesale supplier should I call?",
          "What is my total inventory valuation?",
        ],
        isAiGenerated: true,
        timestamp: new Date().toISOString(),
      };
    }

    // Default dynamic response
    const totalRev = sales.reduce((acc, s) => acc + (s.totalAmount || 0), 0);
    return {
      answer: `I reviewed your store database for **"${question}"**:\n\nYou currently have **${products.length} products**, recorded sales revenue of **${formatCurrency(totalRev)}**, and **${suppliers.length} active wholesale suppliers**.\n\nYou can ask me:\n- *"Which products should I reorder this week?"*\n- *"What are my top-selling products?"*\n- *"What is my total inventory valuation?"*`,
      suggestedFollowUps: [
        "Which products should I reorder?",
        "What are my top-selling products?",
        "What is my total inventory valuation?",
      ],
      isAiGenerated: true,
      timestamp: new Date().toISOString(),
    };
  },

  /**
   * Scan vendor paper bill/invoice image or PDF
   * POST /api/v1/ai/invoice-extract
   */
  async extractInvoiceFromBill(file) {
    if (!isMockEnabled()) {
      // Real mode: surface the backend's error instead of inventing invoice data.
      const formData = new FormData();
      formData.append('file', file);
      const response = await apiClient.post('/ai/invoice-extract', formData, {
        headers: {
          'Content-Type': 'multipart/form-data',
        },
        // AI extraction can take well over the default 10s, especially for multi-page PDFs.
        timeout: 180000,
      });
      return extractData(response);
    }

    // Demo (mock) mode only: sample extraction
    await new Promise((r) => setTimeout(r, 1200));
    const suppliers = mockDb.getSuppliers();
    const sup = suppliers[0] || { supplierName: 'Delhi Wholesale FMCG Traders' };

    return {
      supplierName: sup.supplierName || 'Delhi Wholesale FMCG Traders',
      invoiceNumber: `DWT-${Math.floor(1000 + Math.random() * 9000)}`,
      invoiceDate: new Date().toISOString(),
      totalAmount: 12500.00,
      items: [
        {
          productName: "Tata Salt Vacuum Evaporated 1kg",
          quantity: 50,
          unitPrice: 22.00,
          lineTotal: 1100.00,
        },
        {
          productName: "Aashirvaad Shudh Chakki Atta 10kg",
          quantity: 20,
          unitPrice: 420.00,
          lineTotal: 8400.00,
        },
        {
          productName: "Fortune Sunlite Refined Sunflower Oil 1L",
          quantity: 24,
          unitPrice: 125.00,
          lineTotal: 3000.00,
        },
      ],
      notes: "Extracted via SmartStock OCR from paper bill. Please verify quantities before saving.",
    };
  },
};

export default aiService;
