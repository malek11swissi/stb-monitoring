/** Ajoute le JWT uniquement aux API privées et nettoie les sessions révoquées. */
import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from './auth.service';

export const authInterceptor:HttpInterceptorFn=(req,next)=>{
  const auth=inject(AuthService);const router=inject(Router);
  const publicAuthRoutes=['/api/auth/login','/api/auth/forgot-password','/api/auth/reset-password'];
  const isPublic=publicAuthRoutes.some(path=>req.url.includes(path));
  const token=isPublic?null:localStorage.getItem('stb_token');
  const request=token?req.clone({setHeaders:{Authorization:`Bearer ${token}`}}):req;
  return next(request).pipe(catchError((error:HttpErrorResponse)=>{
    if(error.status===401&&!isPublic){auth.logout();router.navigate(['/login'],{queryParams:{session:'expired'}});}
    return throwError(()=>error);
  }));
};
