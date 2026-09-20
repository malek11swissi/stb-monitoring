// Angular utilise le port API local pendant `ng serve`. En conteneur, l'API

const localDevelopment = globalThis.location?.hostname === 'localhost'
//port angular 
  && globalThis.location?.port === '4200';

export const API_ROOT = localDevelopment ? 'http://localhost:5041/api' : '/api';
export const API_ORIGIN = localDevelopment ? 'http://localhost:5041' : ''; // pour les photos exp: /uploads/avatars/photo.webp
