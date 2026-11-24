import apiClient, { extractData, isMockEnabled } from './api';

export const supplierInvoiceService = {
  async getInvoices(params = {}) {
    if (!isMockEnabled()) {
      try {
        const response = await apiClient.get('/supplier-invoices', { params });
        const data = extractData(response);
        return {
          items: data?.items || [],
          totalCount: data?.totalCount ?? (data?.items?.length || 0),
        };
      } catch (err) {
        console.warn('GET /supplier-invoices failed:', err);
      }
    }
    return { items: [], totalCount: 0 };
  },

  async getInvoiceById(id) {
    if (!isMockEnabled()) {
      try {
        const response = await apiClient.get(`/supplier-invoices/${id}`);
        return extractData(response);
      } catch (err) {
        console.warn(`GET /supplier-invoices/${id} failed:`, err);
        throw err;
      }
    }
    throw new Error('Invoice not found');
  },

  async uploadInvoice(file, supplierId = null) {
    const formData = new FormData();
    formData.append('file', file);
    if (supplierId) {
      formData.append('supplierId', supplierId);
    }

    const response = await apiClient.post('/supplier-invoices/upload', formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
      // Multi-page OCR extraction and Gemini multimodal calls can take up to several minutes.
      timeout: 300000, // 5 minutes
    });
    return extractData(response);
  },

  async rematchProducts(id) {
    const response = await apiClient.post(`/supplier-invoices/${id}/rematch`, null, {
      timeout: 180000,
    });
    return extractData(response);
  },

  async updateItem(invoiceId, itemId, data) {
    const response = await apiClient.put(`/supplier-invoices/${invoiceId}/items/${itemId}`, data);
    return extractData(response);
  },

  async confirmMatch(invoiceId, data) {
    const response = await apiClient.post(`/supplier-invoices/${invoiceId}/confirm-match`, data);
    return extractData(response);
  },

  async createProductFromInvoice(invoiceId, data) {
    const response = await apiClient.post(`/supplier-invoices/${invoiceId}/add-product`, data);
    return extractData(response);
  },

  async approveInvoice(invoiceId, data) {
    const response = await apiClient.post(`/supplier-invoices/${invoiceId}/approve`, data, {
      timeout: 180000,
    });
    return extractData(response);
  },

  async rejectInvoice(invoiceId, reason = null) {
    const response = await apiClient.post(`/supplier-invoices/${invoiceId}/reject`, null, {
      params: { reason },
      timeout: 60000,
    });
    return extractData(response);
  },

  async getDashboardMetrics() {
    if (!isMockEnabled()) {
      try {
        const response = await apiClient.get('/supplier-invoices/dashboard-metrics');
        return extractData(response);
      } catch (err) {
        console.warn('GET /supplier-invoices/dashboard-metrics failed:', err);
      }
    }
    return {
      totalInvoices: 0,
      readyForReviewCount: 0,
      approvedCount: 0,
      rejectedCount: 0,
      duplicateCount: 0,
      totalAmountThisMonth: 0,
      totalPriceHikesDetected: 0,
    };
  },

  async compareInvoice(id) {
    const response = await apiClient.get(`/supplier-invoices/${id}/compare`, {
      timeout: 120000,
    });
    return extractData(response);
  },

  async queryAssistant(query, supplierId = null, invoiceId = null) {
    const response = await apiClient.post(
      '/supplier-invoices/assistant-query',
      {
        query,
        supplierId,
        invoiceId,
      },
      {
        timeout: 180000,
      }
    );
    return extractData(response);
  },
};

export default supplierInvoiceService;
