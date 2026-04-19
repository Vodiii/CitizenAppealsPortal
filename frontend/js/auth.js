import { api } from './api.js';

export const auth = {
  token: null,
  userRoles: [],

  init() {
    this.token = localStorage.getItem('token');
    if (this.token) {
      try {
        const payload = JSON.parse(atob(this.token.split('.')[1]));
        this.userRoles = payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || [];
      } catch (e) {
        console.warn('Invalid token');
        this.logout();
      }
    }
  },

  async login(email, password) {
    const data = await api.login(email, password);
    this.token = data.token;
    this.userRoles = data.roles || [];
    localStorage.setItem('token', this.token);
    return data;
  },

  logout() {
    this.token = null;
    this.userRoles = [];
    localStorage.removeItem('token');
    window.location.hash = '#/';
  },

  isAuthenticated() {
    return !!this.token;
  },

  hasRole(role) {
    return this.userRoles.includes(role);
  },

  getCurrentUserId() {
    if (!this.token) return null;
    const payload = JSON.parse(atob(this.token.split('.')[1]));
    return payload.sub;
  },

  getUserName() {
    if (!this.token) return '';
    const payload = JSON.parse(atob(this.token.split('.')[1]));
    return payload.fullName || payload.email;
  }
};