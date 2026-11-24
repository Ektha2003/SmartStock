import apiClient, { extractData } from './api';

export const recurringOrderService = {
  async getRecurringOrders(params = {}) {
    const response = await apiClient.get('/recurring-orders', { params });
    return extractData(response);
  },

  async getRecurringOrder(id) {
    const response = await apiClient.get(`/recurring-orders/${id}`);
    return extractData(response);
  },

  async generateDraftOrder(customerId, scheduledDate = null) {
    const url = scheduledDate
      ? `/recurring-orders/generate-draft/${customerId}?scheduledDate=${encodeURIComponent(scheduledDate)}`
      : `/recurring-orders/generate-draft/${customerId}`;
    const response = await apiClient.post(url);
    return extractData(response);
  },

  async finalizeOrderToSale(id) {
    const response = await apiClient.post(`/recurring-orders/${id}/finalize-to-sale`);
    return extractData(response);
  },

  async cancelOrder(id, reason = null) {
    const url = reason
      ? `/recurring-orders/${id}/cancel?reason=${encodeURIComponent(reason)}`
      : `/recurring-orders/${id}/cancel`;
    const response = await apiClient.post(url);
    return extractData(response);
  },

  async resolveManualReview(id, data) {
    const response = await apiClient.post(`/recurring-orders/${id}/resolve-review`, data);
    return extractData(response);
  },

  async getDashboardMetrics() {
    const response = await apiClient.get('/recurring-orders/dashboard-metrics');
    return extractData(response);
  },

  async triggerSchedulerCycle() {
    const response = await apiClient.post('/recurring-orders/trigger-scheduler-cycle');
    return extractData(response);
  },

  // Public Customer Approval Portal
  async getOrderByToken(token) {
    const response = await apiClient.get(`/customer-portal/order-by-token/${token}`);
    return extractData(response);
  },

  async submitOrderDecision(token, data) {
    const response = await apiClient.post(`/customer-portal/submit-order/${token}`, data);
    return extractData(response);
  },

  // Inbound Email Simulation & Logs
  async simulateReply(data) {
    const response = await apiClient.post('/email-processing/simulate-reply', data);
    return extractData(response);
  },

  async getEmailLogs(params = {}) {
    const response = await apiClient.get('/email-processing/email-logs', { params });
    return extractData(response);
  },

  async getAILogs(params = {}) {
    const response = await apiClient.get('/email-processing/ai-logs', { params });
    return extractData(response);
  },

  async testSendEmail(data) {
    const response = await apiClient.post('/email-processing/test-send', data);
    return extractData(response);
  },
};

export default recurringOrderService;
