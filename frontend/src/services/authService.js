import apiClient, { extractData, isMockEnabled } from './api';
import { mockDb } from './mockData';

const DEFAULT_MOCK_USER = {
  id: "usr_001",
  fullName: "Ramesh Sharma",
  email: "demo@smartstock.in",
  businessId: "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  role: 1, // Owner
  isActive: true,
};

export const authService = {
  async login(credentials) {
    if (!isMockEnabled()) {
      const response = await apiClient.post('/auth/login', {
        email: credentials.email,
        password: credentials.password,
      });
      const data = extractData(response);
      if (data?.token) {
        localStorage.setItem('smartstock_token', data.token);
        if (data.refreshToken) {
          localStorage.setItem('smartstock_refresh_token', data.refreshToken);
        }
        if (data.user) {
          localStorage.setItem('smartstock_user', JSON.stringify(data.user));
        }
        if (data.business) {
          localStorage.setItem('smartstock_business', JSON.stringify(data.business));
          mockDb.saveBusiness(data.business);
        }
      }
      return data;
    }

    // Dynamic mock login
    await new Promise((r) => setTimeout(r, 300));
    const user = {
      ...DEFAULT_MOCK_USER,
      email: credentials.email || DEFAULT_MOCK_USER.email,
    };
    const business = mockDb.getBusiness();
    const token = 'mock-jwt-token-smartstock-session-' + Date.now();
    const refreshToken = 'mock-refresh-token-' + Date.now();

    localStorage.setItem('smartstock_token', token);
    localStorage.setItem('smartstock_refresh_token', refreshToken);
    localStorage.setItem('smartstock_user', JSON.stringify(user));
    localStorage.setItem('smartstock_business', JSON.stringify(business));

    return { token, refreshToken, user, business };
  },

  async register(data) {
    if (!isMockEnabled()) {
      const payload = {
        fullName: data.fullName,
        email: data.email,
        password: data.password,
        businessName: data.businessName,
        ownerName: data.fullName,
        phone: data.phone,
        address: data.address,
        businessType: data.businessType || 'Kirana',
      };
      const response = await apiClient.post('/auth/register', payload);
      const resData = extractData(response);
      if (resData?.token) {
        localStorage.setItem('smartstock_token', resData.token);
        if (resData.refreshToken) {
          localStorage.setItem('smartstock_refresh_token', resData.refreshToken);
        }
        if (resData.user) {
          localStorage.setItem('smartstock_user', JSON.stringify(resData.user));
        }
        if (resData.business) {
          localStorage.setItem('smartstock_business', JSON.stringify(resData.business));
          mockDb.saveBusiness(resData.business);
        }
      }
      return resData;
    }

    // Dynamic mock registration
    await new Promise((r) => setTimeout(r, 400));
    const businessId = `biz_${Date.now()}`;
    const user = {
      id: `usr_${Date.now()}`,
      fullName: data.fullName,
      email: data.email,
      businessId,
      role: 1,
      isActive: true,
    };
    const business = {
      id: businessId,
      businessName: data.businessName,
      ownerName: data.fullName,
      phone: data.phone,
      address: data.address,
      businessType: data.businessType || 'Kirana',
      isActive: true,
    };
    const token = 'mock-jwt-token-smartstock-session-' + Date.now();
    const refreshToken = 'mock-refresh-token-' + Date.now();

    localStorage.setItem('smartstock_token', token);
    localStorage.setItem('smartstock_refresh_token', refreshToken);
    localStorage.setItem('smartstock_user', JSON.stringify(user));
    localStorage.setItem('smartstock_business', JSON.stringify(business));
    mockDb.saveBusiness(business);

    return { token, refreshToken, user, business };
  },

  async getCurrentUser() {
    if (!isMockEnabled()) {
      try {
        const response = await apiClient.get('/auth/me');
        const user = extractData(response);
        if (user) {
          localStorage.setItem('smartstock_user', JSON.stringify(user));
          return user;
        }
      } catch (err) {
        console.warn('GET /auth/me failed, reading cached user:', err);
      }
    }
    const stored = localStorage.getItem('smartstock_user');
    return stored ? JSON.parse(stored) : DEFAULT_MOCK_USER;
  },

  async refreshToken() {
    const refreshToken = localStorage.getItem('smartstock_refresh_token');
    if (!refreshToken) throw new Error('No refresh token available');
    const response = await apiClient.post('/auth/refresh', { refreshToken });
    const data = extractData(response);
    if (data.token) {
      localStorage.setItem('smartstock_token', data.token);
      if (data.refreshToken) {
        localStorage.setItem('smartstock_refresh_token', data.refreshToken);
      }
    }
    return data;
  },

  logout() {
    localStorage.removeItem('smartstock_token');
    localStorage.removeItem('smartstock_refresh_token');
    localStorage.removeItem('smartstock_user');
    localStorage.removeItem('smartstock_business');
  },

  isAuthenticated() {
    return !!localStorage.getItem('smartstock_token');
  },
};

export default authService;
