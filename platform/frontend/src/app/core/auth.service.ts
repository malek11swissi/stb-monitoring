/** État de session Angular : JWT, utilisateur courant, rôles et permissions d'affichage. */
import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
export interface User { id:string; username:string; email:string; firstName:string; lastName:string; role:string; roles?:string[]; isActive:boolean; createdAt:string; lastLoginAt?:string; avatarUrl?:string; phoneNumber?:string; jobTitle?:string; skills:string[]; badge:string; resolvedIncidents:number; }
export interface LoginResponse { token?:string; expiresAt?:string; user?:User; requiresTwoFactor:boolean; challengeToken?:string; }
@Injectable({providedIn:'root'})
export class AuthService {
  private readonly api='http://localhost:5041/api';
  readonly currentUser=signal<User|null>(this.readUser());
  constructor(private http:HttpClient){}
  login(usernameOrEmail:string,password:string):Observable<LoginResponse>{return this.http.post<LoginResponse>(`${this.api}/auth/login`,{usernameOrEmail,password}).pipe(tap(x=>this.persistSession(x)));}
  verifyTwoFactor(challengeToken:string,code:string):Observable<LoginResponse>{return this.http.post<LoginResponse>(`${this.api}/auth/verify-2fa`,{challengeToken,code}).pipe(tap(x=>this.persistSession(x)));}
  me(){return this.http.get<User>(`${this.api}/auth/me`).pipe(tap(x=>{x=this.normalize(x);this.currentUser.set(x);localStorage.setItem('stb_user',JSON.stringify(x));}));}
  logout(){localStorage.removeItem('stb_token');localStorage.removeItem('stb_user');this.currentUser.set(null);}
  token(){const token=localStorage.getItem('stb_token');if(!token)return null;try{const payload=JSON.parse(atob(token.split('.')[1].replace(/-/g,'+').replace(/_/g,'/')));if(payload.exp*1000<=Date.now()||payload.session_version===undefined){this.logout();return null;}}catch{this.logout();return null;}return token;}
  hasPermission(permission:string){
    const role=this.currentUser()?.role;
    if(!role)return false;
    const access:Record<string,string[]>={
      'users.read':['ADMIN'],'users.manage':['ADMIN'],'audit.read':['ADMIN'],
      'systems.read':['ADMIN','SUPERVISOR','TECHNICIAN','MANAGER_IT'],'systems.manage':['ADMIN'],'endpoints.manage':['SUPERVISOR'],
      'checks.read':['SUPERVISOR','TECHNICIAN'],'checks.execute':['SUPERVISOR'],
      'alerts.read':['SUPERVISOR'],'alerts.acknowledge':['SUPERVISOR'],'alert_rules.manage':['SUPERVISOR'],
      'incidents.read':['ADMIN','SUPERVISOR','TECHNICIAN','MANAGER_IT'],'incidents.manage':['SUPERVISOR'],
      'incidents.assign':['SUPERVISOR'],'incidents.work':['TECHNICIAN'],'incidents.resolve':['TECHNICIAN'],
      'incidents.close':['SUPERVISOR'],'incidents.archive':['SUPERVISOR'],'incidents.delete':['ADMIN'],
      'sla.manage':['MANAGER_IT'],'notifications.read':['ADMIN','SUPERVISOR','TECHNICIAN','MANAGER_IT'],
      'notifications.manage':['ADMIN'],'maintenance.read':['SUPERVISOR','TECHNICIAN','MANAGER_IT'],'maintenance.manage':['SUPERVISOR'],
      'reporting.read':['ADMIN','SUPERVISOR','TECHNICIAN','MANAGER_IT'],'reporting.export':['MANAGER_IT']
    };
    return access[permission]?.includes(role)??false;
  }
  private normalize(user:User):User{const role=user.role||user.roles?.[0]||'';return {...user,role,roles:role?[role]:[]}}
  private persistSession(response:LoginResponse){if(!response.token||!response.user)return;const user=this.normalize(response.user);localStorage.setItem('stb_token',response.token);localStorage.setItem('stb_user',JSON.stringify(user));this.currentUser.set(user)}
  private readUser():User|null{try{const user=JSON.parse(localStorage.getItem('stb_user')||'null');return user?this.normalize(user):null;}catch{return null;}}
}
