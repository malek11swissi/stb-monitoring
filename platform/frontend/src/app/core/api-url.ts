// Angular utilise le port API local pendant `ng serve`. En conteneur, l'API
// est publiée par le même Ingress sous /api.
const localDevelopment = globalThis.location?.hostname === 'localhost'
  && globalThis.location?.port === '4200';

export const API_ROOT = localDevelopment ? 'http://localhost:5041/api' : '/api';
export const API_ORIGIN = localDevelopment ? 'http://localhost:5041' : '';
