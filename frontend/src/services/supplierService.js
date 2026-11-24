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
            items: data.items.map(normalizeSupplier),
            totalCount: data.totalCount ?? data.items.length,
          };
        }
        if (Array.isArray(data)) {
          return {
            items: data.map(normalizeSupplier),
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
          (s.companyName || '').toLowerCase().includes(q) ||
          (s.contactPerson || '').toLowerCase().includes(q) ||
          (s.gstNumber || s.gstin || '').toLowerCase().includes(q) ||
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

  async getSupplierDetail(id) {
    if (!isMockEnabled()) {
      try {
        const response = await apiClient.get(`/suppliers/${id}/detail`);
        return extractData(response);
      } catch (err) {
        console.warn(`GET /suppliers/${id}/detail failed:`, err);
      }
    }
    const supplier = await this.getSupplierById(id);
    return {
      ...supplier,
      recentInvoices: [],
      recentPurchases: [],
      suppliedProducts: [],
    };
  },

  async getSuppliedProducts(id) {
    if (!isMockEnabled()) {
      try {
        const response = await apiClient.get(`/suppliers/${id}/products`);
        return extractData(response);
      } catch (err) {
        console.warn(`GET /suppliers/${id}/products failed:`, err);
      }
    }
    return [];
  },

  async getSupplierInvoices(id, params = {}) {
    if (!isMockEnabled()) {
      try {
        const response = await apiClient.get(`/suppliers/${id}/invoices`, { params });
        return extractData(response);
      } catch (err) {
        console.warn(`GET /suppliers/${id}/invoices failed:`, err);
      }
    }
    return { items: [], totalCount: 0 };
  },

  async getSupplierPurchases(id, params = {}) {
    if (!isMockEnabled()) {
      try {
        const response = await apiClient.get(`/suppliers/${id}/purchases`, { params });
        return extractData(response);
      } catch (err) {
        console.warn(`GET /suppliers/${id}/purchases failed:`, err);
      }
    }
    return { items: [], totalCount: 0 };
  },

  async createSupplier(data) {
    const payload = {
      supplierName: data.supplierName || data.name,
      companyName: data.companyName || null,
      contactPerson: data.contactPerson,
      phone: data.phone,
      email: data.email || '',
      gstNumber: data.gstNumber || data.gstin || null,
      panNumber: data.panNumber || null,
      billingAddress: data.billingAddress || null,
      address: data.address || '',
      paymentTerms: data.paymentTerms || null,
      notes: data.notes || null,
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
      companyName: data.companyName || null,
      contactPerson: data.contactPerson,
      phone: data.phone,
      email: data.email || '',
      gstNumber: data.gstNumber || data.gstin || null,
      panNumber: data.panNumber || null,
      billingAddress: data.billingAddress || null,
      address: data.address || '',
      paymentTerms: data.paymentTerms || null,
      notes: data.notes || null,
      isActive: data.status === 'Inactive' ? false : data.isActive ?? true,
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
  const name = s.supplierName || s.name || 'Wholesale Supplier';
  const gstin = s.gstNumber || s.gstin || '';
  return {
    ...s,
    id: s.id,
    name,
    supplierName: name,
    companyName: s.companyName || '',
    contactPerson: s.contactPerson || '',
    phone: s.phone || '',
    email: s.email || '',
    address: s.address || '',
    billingAddress: s.billingAddress || '',
    gstin,
    gstNumber: gstin,
    panNumber: s.panNumber || '',
    paymentTerms: s.paymentTerms || '',
    notes: s.notes || '',
    invoicesCount: s.invoicesCount || 0,
    purchasesCount: s.purchasesCount || 0,
    status: s.isActive === false ? 'Inactive' : 'Active',
  };
}

export default supplierService;
