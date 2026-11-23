import apiClient, { extractData, isMockEnabled } from './api';
import { mockDb } from './mockData';

export const supplierService = {
  async getSuppliers(params = {}) {
    if (!isMockEnabled()) {
      try {
        const response = await apiClient.get('/suppliers', { params });
        const data = extractData(response);
        if (data?.items && Array.isArray(data.items)) {
          return {
            items: data.items,
            totalCount: data.totalCount ?? data.items.length,
          };
        }
        if (Array.isArray(data)) {
          return {
            items: data,
            totalCount: data.length,
          };
        }
      } catch (err) {
        console.warn('GET /suppliers failed, reading dynamic store:', err);
      }
    }

    await new Promise((r) => setTimeout(r, 150));
    let suppliers = mockDb.getSuppliers();

    if (params.search) {
      const q = params.search.toLowerCase();
      suppliers = suppliers.filter(
        (s) =>
          (s.supplierName || s.name || '').toLowerCase().includes(q) ||
          (s.contactPerson || '').toLowerCase().includes(q) ||
          (s.phone || '').includes(q)
      );
    }

    return {
      items: suppliers.map(normalizeSupplier),
      totalCount: suppliers.length,
    };
  },

  async getSupplierById(id) {
    if (!isMockEnabled()) {
      try {
        const response = await apiClient.get(`/suppliers/${id}`);
        return normalizeSupplier(extractData(response));
      } catch (err) {
        console.warn(`GET /suppliers/${id} failed:`, err);
      }
    }
    const suppliers = mockDb.getSuppliers();
    const supplier = suppliers.find((s) => String(s.id) === String(id));
    if (!supplier) throw new Error('Supplier not found');
    return normalizeSupplier(supplier);
  },

  async createSupplier(data) {
    const payload = {
      supplierName: data.supplierName || data.name,
      contactPerson: data.contactPerson,
      phone: data.phone,
      email: data.email || '',
      address: data.address,
    };

    if (!isMockEnabled()) {
      try {
        const response = await apiClient.post('/suppliers', payload);
        return normalizeSupplier(extractData(response));
      } catch (err) {
        console.warn('POST /suppliers failed:', err);
        throw err;
      }
    }

    await new Promise((r) => setTimeout(r, 200));
    const suppliers = mockDb.getSuppliers();
    const newSupplier = {
      id: `sup-${Date.now()}`,
      ...payload,
      name: payload.supplierName,
      isActive: true,
    };
    suppliers.push(newSupplier);
    mockDb.saveSuppliers(suppliers);
    return newSupplier;
  },

  async updateSupplier(id, data) {
    const payload = {
      supplierName: data.supplierName || data.name,
      contactPerson: data.contactPerson,
      phone: data.phone,
      email: data.email || '',
      address: data.address,
    };

    if (!isMockEnabled()) {
      try {
        const response = await apiClient.put(`/suppliers/${id}`, payload);
        return normalizeSupplier(extractData(response));
      } catch (err) {
        console.warn(`PUT /suppliers/${id} failed:`, err);
        throw err;
      }
    }

    await new Promise((r) => setTimeout(r, 150));
    const suppliers = mockDb.getSuppliers();
    const index = suppliers.findIndex((s) => String(s.id) === String(id));
    if (index === -1) throw new Error('Supplier not found');
    suppliers[index] = { ...suppliers[index], ...payload, name: payload.supplierName };
    mockDb.saveSuppliers(suppliers);
    return suppliers[index];
  },

  async deleteSupplier(id) {
    if (!isMockEnabled()) {
      try {
        const response = await apiClient.delete(`/suppliers/${id}`);
        return extractData(response);
      } catch (err) {
        console.warn(`DELETE /suppliers/${id} failed:`, err);
        throw err;
      }
    }

    await new Promise((r) => setTimeout(r, 150));
    let suppliers = mockDb.getSuppliers();
    suppliers = suppliers.filter((s) => String(s.id) !== String(id));
    mockDb.saveSuppliers(suppliers);
    return { success: true };
  },
};

function normalizeSupplier(s) {
  if (!s) return s;
  return {
    ...s,
    name: s.supplierName || s.name,
    supplierName: s.supplierName || s.name,
  };
}

export default supplierService;
