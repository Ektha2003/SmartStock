import axios from 'axios';

const DEFAULT_API_BASE = 'http://localhost:5000/api/v1';
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || DEFAULT_API_BASE;

export const apiClient = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
  timeout: 10000,
});

// Request interceptor to inject Bearer token
apiClient.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('smartstock_token');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// Response interceptor with token refresh, response unwrapping, and standard error handling
let isRefreshing = false;
let failedQueue = [];

const processQueue = (error, token = null) => {
  failedQueue.forEach((prom) => {
    if (error) {
      prom.reject(error);
    } else {
      prom.resolve(token);
    }
  });
  failedQueue = [];
};

apiClient.interceptors.response.use(
  (response) => {
    return response;
  },
  async (error) => {
    const originalRequest = error.config;

    // Handle 401 Unauthorized with Refresh Token
    if (error.response && error.response.status === 401 && !originalRequest._retry) {
      const refreshToken = localStorage.getItem('smartstock_refresh_token');

      if (refreshToken && originalRequest.url !== '/auth/refresh' && originalRequest.url !== '/auth/login') {
        if (isRefreshing) {
          return new Promise((resolve, reject) => {
            failedQueue.push({ resolve, reject });
          })
            .then((token) => {
              originalRequest.headers.Authorization = `Bearer ${token}`;
              return apiClient(originalRequest);
            })
            .catch((err) => Promise.reject(err));
        }

        originalRequest._retry = true;
        isRefreshing = true;

        try {
          const refreshRes = await axios.post(`${API_BASE_URL}/auth/refresh`, {
            refreshToken,
          });

          const data = refreshRes.data?.data || refreshRes.data;
          const newToken = data.token;
          const newRefreshToken = data.refreshToken;

          if (newToken) {
            localStorage.setItem('smartstock_token', newToken);
            if (newRefreshToken) {
              localStorage.setItem('smartstock_refresh_token', newRefreshToken);
            }
            apiClient.defaults.headers.common.Authorization = `Bearer ${newToken}`;
            processQueue(null, newToken);
            originalRequest.headers.Authorization = `Bearer ${newToken}`;
            return apiClient(originalRequest);
          }
        } catch (refreshErr) {
          processQueue(refreshErr, null);
          localStorage.removeItem('smartstock_token');
          localStorage.removeItem('smartstock_refresh_token');
          localStorage.removeItem('smartstock_user');
          if (window.location.pathname !== '/login' && window.location.pathname !== '/register') {
            window.location.href = '/login';
          }
          return Promise.reject(refreshErr);
        } finally {
          isRefreshing = false;
        }
      } else {
        localStorage.removeItem('smartstock_token');
        localStorage.removeItem('smartstock_refresh_token');
        localStorage.removeItem('smartstock_user');
        if (window.location.pathname !== '/login' && window.location.pathname !== '/register') {
          window.location.href = '/login';
        }
      }
    }

    // Standard Error formatting
    if (error.response?.data) {
      const resData = error.response.data;
      if (resData.errors && Array.isArray(resData.errors) && resData.errors.length > 0) {
        const errorMessages = resData.errors
          .map((e) => (typeof e === 'string' ? e : e.message || `${e.field}: ${e.message}`))
          .join(', ');
        return Promise.reject(new Error(errorMessages || resData.message || 'Validation failed'));
      }
      return Promise.reject(new Error(resData.message || 'Server request failed'));
    } else if (error.request) {
      return Promise.reject(new Error('Unable to connect to SmartStock backend. Is the server running?'));
    }

    return Promise.reject(error);
  }
);

/**
 * Unwrap ApiResponse<T>
 */
export function extractData(response) {
  if (response && response.data !== undefined) {
    if (response.data && response.data.data !== undefined) {
      return response.data.data;
    }
    return response.data;
  }
  return response;
}

export const isMockEnabled = () => import.meta.env.VITE_USE_MOCK === 'true';

export default apiClient;
