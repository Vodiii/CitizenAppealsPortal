import { auth } from './auth.js';

export class Router {
  constructor(routes) {
    this.routes = routes;
    window.addEventListener('hashchange', () => this.handleRoute());
    window.addEventListener('load', () => this.handleRoute());
  }

  handleRoute() {
    const hash = window.location.hash.slice(1) || '/';
    let route = this.routes[hash];
    let params = null;

    if (!route) {
      for (const [pattern, handler] of Object.entries(this.routes)) {
        const match = this.matchRoute(pattern, hash);
        if (match) {
          route = handler;
          params = match;
          break;
        }
      }
    }

    if (!route) {
      route = this.routes['/404'];
    }

    if (route) {
      if (route.auth && !auth.isAuthenticated()) {
        window.location.hash = '#/login';
        return;
      }
      if (route.roles && !route.roles.some(r => auth.hasRole(r))) {
        window.location.hash = '#/';
        return;
      }
      route.handler(params);
    }

    if (typeof updateNavigation === 'function') {
      updateNavigation();
    }
  }

  matchRoute(pattern, path) {
    const pathWithoutQuery = path.split('?')[0];
    const patternParts = pattern.split('/');
    const pathParts = pathWithoutQuery.split('/');

    if (patternParts.length !== pathParts.length) return null;

    const params = {};
    for (let i = 0; i < patternParts.length; i++) {
      if (patternParts[i].startsWith(':')) {
        const paramName = patternParts[i].slice(1);
        params[paramName] = pathParts[i];
      } else if (patternParts[i] !== pathParts[i]) {
        return null;
      }
    }
    return params;
  }
}
