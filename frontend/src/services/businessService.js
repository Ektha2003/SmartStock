import apiClient, { extractData, isMockEnabled } from './api';
import { mockDb } from './mockData';

export const businessService = {
  async getBusiness() {
    if (!isMockEnabled()) {
      try {
        const res = await apiClient.get('/business');
        return extractData(res);
      } catch (err) {
        console.warn('Backend /business error, falling back to local business:', err);
      }
    }
    await new Promise((r) => setTimeout(r, 150));
    return mockDb.getBusiness();
  },

  async updateBusiness(data) {
    if (!isMockEnabled()) {
      try {
        const payload = {
          businessName: data.businessName,
          ownerName: data.ownerName || data.fullName,
          phone: data.phone,
          address: data.address,
          businessType: data.businessType || 'Kirana / Grocery',
        };
        const res = await apiClient.put('/business', payload);
        const updated = extractData(res) || payload;
        // Keep in local cache too
        mockDb.saveBusiness({ ...mockDb.getBusiness(), ...updated });
        return updated;
      } catch (err) {
        console.warn('Backend PUT /business failed, falling back to local update:', err);
      }
    }
    await new Promise((r) => setTimeout(r, 200));
    const current = mockDb.getBusiness();
    const updated = { ...current, ...data };
    mockDb.saveBusiness(updated);
    return updated;
  },
};

export default businessService;

