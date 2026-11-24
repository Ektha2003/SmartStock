import apiClient, { extractData } from './api';

export const customerService = {
  async getCustomers(params = {}) {
    const response = await apiClient.get('/customers', { params });
    return extractData(response);
  },

  async getCustomer(id) {
    const response = await apiClient.get(`/customers/${id}`);
    return extractData(response);
  },

  async createCustomer(data) {
    const response = await apiClient.post('/customers', data);
    return extractData(response);
  },

  async updateCustomer(id, data) {
    const response = await apiClient.put(`/customers/${id}`, data);
    return extractData(response);
  },

  async toggleCustomerStatus(id) {
    const response = await apiClient.patch(`/customers/${id}/toggle-status`);
    return extractData(response);
  },

  async deleteCustomer(id) {
    const response = await apiClient.delete(`/customers/${id}`);
    return extractData(response);
  },

  async getCustomerOrders(id, params = {}) {
    const response = await apiClient.get(`/customers/${id}/orders`, { params });
    return extractData(response);
  },

  async getCustomerUpcomingOrders(id) {
    const response = await apiClient.get(`/customers/${id}/upcoming-orders`);
    return extractData(response);
  },

  // Recurring Products
  async getRecurringProducts(customerId) {
    const response = await apiClient.get(`/customers/${customerId}/recurring-products`);
    return extractData(response);
  },

  async addRecurringProduct(customerId, data) {
    const response = await apiClient.post(`/customers/${customerId}/recurring-products`, data);
    return extractData(response);
  },

  async updateRecurringProduct(customerId, productId, data) {
    const response = await apiClient.put(`/customers/${customerId}/recurring-products/${productId}`, data);
    return extractData(response);
  },

  async removeRecurringProduct(customerId, productId) {
    const response = await apiClient.delete(`/customers/${customerId}/recurring-products/${productId}`);
    return extractData(response);
  },

  // Recurring Settings
  async getRecurringSetting(customerId) {
    const response = await apiClient.get(`/customers/${customerId}/recurring-settings`);
    return extractData(response);
  },

  async updateRecurringSetting(customerId, data) {
    const response = await apiClient.put(`/customers/${customerId}/recurring-settings`, data);
    return extractData(response);
  },

  async toggleSubscriptionStatus(customerId, status) {
    const response = await apiClient.patch(`/customers/${customerId}/recurring-settings/status?status=${status}`);
    return extractData(response);
  },
};

export default customerService;
