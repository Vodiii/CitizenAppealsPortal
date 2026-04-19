const API_BASE = 'http://localhost:5000/api';

async function apiRequest(endpoint, options = {}) {
  const token = localStorage.getItem('token');
  const headers = { ...options.headers };
  if (!(options.body instanceof FormData)) {
    headers['Content-Type'] = 'application/json';
  }
  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }

  const config = { ...options, headers };
  try {
    const response = await fetch(`${API_BASE}${endpoint}`, config);
    if (!response.ok) {
      const error = await response.text();
      throw new Error(error || `Ошибка ${response.status}`);
    }
    if (response.status === 204) return null;
    return await response.json();
  } catch (error) {
    console.error('API Error:', error);
    throw error;
  }
}

export const api = {
  // Auth
  login: (email, password) => apiRequest('/Auth/login', {
    method: 'POST',
    body: JSON.stringify({ email, password })
  }),
  register: (userData) => apiRequest('/Auth/register', {
    method: 'POST',
    body: JSON.stringify(userData)
  }),

  // Appeals
  getAppeals: (params = {}) => {
    const query = new URLSearchParams(params).toString();
    return apiRequest(`/Appeals${query ? '?'+query : ''}`);
  },
  getAppeal: (id) => apiRequest(`/Appeals/${id}`),
  createAppeal: (formData) => apiRequest('/Appeals', {
    method: 'POST',
    body: formData
  }),
  updateStatus: (id, newStatus) => apiRequest(`/Appeals/${id}/status`, {
    method: 'PUT',
    body: JSON.stringify({ newStatus })
  }),
  addResponse: (id, content) => apiRequest(`/Appeals/${id}/respond`, {
    method: 'POST',
    body: JSON.stringify({ content })
  }),
  deleteAppeal: (id) => apiRequest(`/Appeals/${id}`, { method: 'DELETE' }),

  // Districts
  getDistricts: () => apiRequest('/Districts'),
  createDistrict: (data) => apiRequest('/Districts', {
    method: 'POST',
    body: JSON.stringify(data)
  }),
  updateDistrict: (id, data) => apiRequest(`/Districts/${id}`, {
    method: 'PUT',
    body: JSON.stringify(data)
  }),
  deleteDistrict: (id) => apiRequest(`/Districts/${id}`, { method: 'DELETE' }),

  // Admin
  getPendingDeputies: () => apiRequest('/admin/deputies/pending'),
  approveDeputy: (id, approve, districtId) => apiRequest(`/admin/deputies/${id}/approve`, {
    method: 'POST',
    body: JSON.stringify({ approve, districtId })
  }),
  getCategories: () => apiRequest('/admin/categories'),
  createCategory: (data) => apiRequest('/admin/categories', {
    method: 'POST',
    body: JSON.stringify(data)
  }),
  updateCategory: (id, data) => apiRequest(`/admin/categories/${id}`, {
    method: 'PUT',
    body: JSON.stringify(data)
  }),
  deleteCategory: (id) => apiRequest(`/admin/categories/${id}`, { method: 'DELETE' }),

  // Map
  getDistrictsGeoJson: () => apiRequest('/map/districts/geojson'),
  getAppealsGeoJson: (params) => {
    const query = new URLSearchParams(params).toString();
    return apiRequest(`/map/appeals/geojson${query ? '?'+query : ''}`);
  },
  findDistrict: (geoJson) => apiRequest('/map/find-district', {
    method: 'POST',
    body: JSON.stringify({ geoJson })
  }),

  // Statistics
  getDeputyStats: (params) => {
    const query = new URLSearchParams(params).toString();
    return apiRequest(`/statistics/deputy${query ? '?'+query : ''}`);
  },
  getAdminStats: (params) => {
    const query = new URLSearchParams(params).toString();
    return apiRequest(`/statistics/admin${query ? '?'+query : ''}`);
  }
};
