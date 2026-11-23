import apiClient, { extractData, isMockEnabled } from './api';
import { mockDb } from './mockData';

export const categoryService = {
  async getCategories() {
    if (!isMockEnabled()) {
      try {
        const response = await apiClient.get('/categories');
        const data = extractData(response);
        if (Array.isArray(data)) return data;
        if (data?.items && Array.isArray(data.items)) return data.items;
      } catch (err) {
        console.warn('GET /categories failed, falling back to dynamic store:', err);
      }
    }

    await new Promise((r) => setTimeout(r, 100));
    const categories = mockDb.getCategories();
    const products = mockDb.getProducts();
    return categories.map((cat) => ({
      ...cat,
      productCount: products.filter((p) => String(p.categoryId) === String(cat.id)).length,
    }));
  },

  async getCategoryById(id) {
    if (!isMockEnabled()) {
      try {
        const response = await apiClient.get(`/categories/${id}`);
        return extractData(response);
      } catch (err) {
        console.warn(`GET /categories/${id} failed:`, err);
      }
    }
    const categories = mockDb.getCategories();
    const cat = categories.find((c) => String(c.id) === String(id));
    if (!cat) throw new Error('Category not found');
    return cat;
  },

  async createCategory(data) {
    if (!isMockEnabled()) {
      try {
        const response = await apiClient.post('/categories', {
          name: data.name,
          description: data.description || '',
        });
        const created = extractData(response);
        const categories = mockDb.getCategories();
        categories.push(created);
        mockDb.saveCategories(categories);
        return created;
      } catch (err) {
        console.warn('POST /categories failed:', err);
        throw err;
      }
    }

    await new Promise((r) => setTimeout(r, 150));
    const categories = mockDb.getCategories();
    const newCat = {
      id: `cat-${Date.now()}`,
      name: data.name,
      description: data.description || '',
      productCount: 0,
    };
    categories.push(newCat);
    mockDb.saveCategories(categories);
    return newCat;
  },

  async updateCategory(id, data) {
    if (!isMockEnabled()) {
      try {
        const response = await apiClient.put(`/categories/${id}`, {
          name: data.name,
          description: data.description || '',
        });
        const updated = extractData(response);
        return updated;
      } catch (err) {
        console.warn(`PUT /categories/${id} failed:`, err);
        throw err;
      }
    }

    await new Promise((r) => setTimeout(r, 150));
    const categories = mockDb.getCategories();
    const index = categories.findIndex((c) => String(c.id) === String(id));
    if (index === -1) throw new Error('Category not found');
    categories[index] = { ...categories[index], ...data };
    mockDb.saveCategories(categories);
    return categories[index];
  },

  async deleteCategory(id) {
    if (!isMockEnabled()) {
      try {
        const response = await apiClient.delete(`/categories/${id}`);
        return extractData(response);
      } catch (err) {
        console.warn(`DELETE /categories/${id} failed:`, err);
        throw err;
      }
    }

    await new Promise((r) => setTimeout(r, 150));
    let categories = mockDb.getCategories();
    categories = categories.filter((c) => String(c.id) !== String(id));
    mockDb.saveCategories(categories);
    return { success: true };
  },
};

export default categoryService;
