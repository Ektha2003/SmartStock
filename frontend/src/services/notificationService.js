import apiClient, { extractData, isMockEnabled } from './api';
import { mockDb } from './mockData';

export const notificationService = {
  async getNotifications(params = {}) {
    if (!isMockEnabled()) {
      try {
        const response = await apiClient.get('/notifications', { params });
        const data = extractData(response);
        if (Array.isArray(data)) return data.map(normalizeNotification);
        if (data?.items && Array.isArray(data.items)) return data.items.map(normalizeNotification);
      } catch (err) {
        console.warn('GET /notifications failed, reading dynamic alerts:', err);
      }
    }

    await new Promise((r) => setTimeout(r, 100));
    let notifs = mockDb.getNotifications().map(normalizeNotification);
    if (params.unreadOnly) {
      notifs = notifs.filter((n) => !n.isRead);
    }
    return notifs;
  },

  async markAsRead(id) {
    if (!isMockEnabled()) {
      try {
        const response = await apiClient.patch(`/notifications/${id}/read`);
        return extractData(response);
      } catch (err) {
        console.warn(`PATCH /notifications/${id}/read failed:`, err);
      }
    }

    await new Promise((r) => setTimeout(r, 100));
    const notifs = mockDb.getNotifications();
    const item = notifs.find((n) => String(n.id) === String(id));
    if (item) {
      item.isRead = true;
      mockDb.saveNotifications(notifs);
    }
    return { success: true };
  },

  async markAllAsRead() {
    if (!isMockEnabled()) {
      try {
        const response = await apiClient.patch('/notifications/read-all');
        return extractData(response);
      } catch (err) {
        console.warn('PATCH /notifications/read-all failed:', err);
      }
    }

    await new Promise((r) => setTimeout(r, 150));
    const notifs = mockDb.getNotifications();
    notifs.forEach((n) => {
      n.isRead = true;
    });
    mockDb.saveNotifications(notifs);
    return { success: true };
  },

  async checkStock() {
    if (!isMockEnabled()) {
      try {
        const response = await apiClient.post('/notifications/check-stock');
        return extractData(response);
      } catch (err) {
        console.warn('POST /notifications/check-stock failed:', err);
      }
    }

    await new Promise((r) => setTimeout(r, 200));
    const products = mockDb.getProducts();
    const notifs = mockDb.getNotifications();

    products.forEach((p) => {
      if (p.currentStock === 0) {
        const exists = notifs.find((n) => String(n.productId) === String(p.id) && !n.isRead);
        if (!exists) {
          notifs.unshift({
            id: `notif-${Date.now()}-${p.id}`,
            productId: p.id,
            productName: p.name,
            title: `Out of Stock: ${p.name}`,
            message: `Current stock (0 ${p.unitName || 'units'}) is exhausted.`,
            notificationType: 2,
            notificationTypeName: 'OutOfStock',
            isRead: false,
            createdAt: new Date().toISOString(),
          });
        }
      }
    });

    mockDb.saveNotifications(notifs);
    return { success: true, message: 'Stock levels re-scanned' };
  },
};

function normalizeNotification(n) {
  if (!n) return n;
  const isOut = n.notificationType === 2 || n.notificationTypeName === 'OutOfStock';
  return {
    ...n,
    severity: isOut ? 'Critical' : 'Warning',
    date: n.createdAt || n.date,
  };
}

export default notificationService;
