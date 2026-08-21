/** Protège les routes privées et redirige les utilisateurs sans droit fonctionnel. */
import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';
export const authGuard:CanActivateFn=(_route,state)=>inject(AuthService).token()?true:inject(Router).createUrlTree(['/login'],{queryParams:{returnUrl:state.url}});
export const permissionGuard:CanActivateFn=(route,state)=>{const auth=inject(AuthService);if(!auth.token())return inject(Router).createUrlTree(['/login'],{queryParams:{returnUrl:state.url}});return auth.hasPermission(route.data['permission'])?true:inject(Router).createUrlTree(['/forbidden']);};
