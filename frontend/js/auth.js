import { api } from './api.js';

// Функция для корректного декодирования JWT с поддержкой UTF-8
function parseJwt(token) {
    try {
        const base64Url = token.split('.')[1];
        const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
        const jsonPayload = decodeURIComponent(atob(base64).split('').map(c => {
            return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
        }).join(''));
        return JSON.parse(jsonPayload);
    } catch (e) {
        console.error('Invalid token', e);
        return null;
    }
}

export const auth = {
    token: null,
    userRoles: [],

    init() {
        this.token = localStorage.getItem('token');
        if (this.token) {
            const payload = parseJwt(this.token);
            if (payload) {
                this.userRoles = payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || [];
            } else {
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
        const payload = parseJwt(this.token);
        return payload ? payload.sub : null;
    },

    getUserName() {
        if (!this.token) return '';
        const payload = parseJwt(this.token);
        return payload ? (payload.fullName || payload.email) : '';
    }
};