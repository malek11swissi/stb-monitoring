import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';
export const authGuard:CanActivateFn=()=>inject(AuthService).token()?true:inject(Router).createUrlTree(['/login']);
export const permissionGuard:CanActivateFn=(route)=>{const auth=inject(AuthService);if(!auth.token())return inject(Router).createUrlTree(['/login']);return auth.hasPermission(route.data['permission'])?true:inject(Router).createUrlTree(['/forbidden']);};
