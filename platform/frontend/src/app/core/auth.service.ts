import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
export interface User { id:string; username:string; email:string; firstName:string; lastName:string; role:string; roles?:string[]; isActive:boolean; createdAt:string; lastLoginAt?:string; }
interface LoginResponse { token:string; expiresAt:string; user:User; }
@Injectable({providedIn:'root'})
export class AuthService {
  private readonly api='http://localhost:5041/api';
  readonly currentUser=signal<User|null>(this.readUser());
  constructor(private http:HttpClient){}
  login(usernameOrEmail:string,password:string):Observable<LoginResponse>{return this.http.post<LoginResponse>(`${this.api}/auth/login`,{usernameOrEmail,password}).pipe(tap(x=>{x.user=this.normalize(x.user);localStorage.setItem('stb_token',x.token);localStorage.setItem('stb_user',JSON.stringify(x.user));this.currentUser.set(x.user);}));}
  me(){return this.http.get<User>(`${this.api}/auth/me`).pipe(tap(x=>{x=this.normalize(x);this.currentUser.set(x);localStorage.setItem('stb_user',JSON.stringify(x));}));}
  logout(){localStorage.removeItem('stb_token');localStorage.removeItem('stb_user');this.currentUser.set(null);}
  token(){const token=localStorage.getItem('stb_token');if(!token)return null;try{const payload=JSON.parse(atob(token.split('.')[1]));if(payload.exp*1000<=Date.now()){this.logout();return null;}}catch{this.logout();return null;}return token;}
  hasPermission(permission:string){
    const role=this.currentUser()?.role;
    if(!role)return false;
    if(role==='ADMIN')return true;
    const access:Record<string,string[]>={
      'systems.read':['SUPERVISOR','TECHNICIAN','MANAGER_IT'],'checks.read':['SUPERVISOR','TECHNICIAN','MANAGER_IT'],
      'alerts.read':['SUPERVISOR','MANAGER_IT'],'incidents.read':['SUPERVISOR','TECHNICIAN','MANAGER_IT'],
      'notifications.read':['SUPERVISOR','TECHNICIAN','MANAGER_IT'],'systems.manage':['SUPERVISOR'],
      'checks.execute':['SUPERVISOR'],'alerts.acknowledge':['SUPERVISOR'],'incidents.manage':['SUPERVISOR'],
      'incidents.assign':['SUPERVISOR'],'incidents.work':['SUPERVISOR','TECHNICIAN'],
      'incidents.resolve':['SUPERVISOR','TECHNICIAN'],'incidents.close':['SUPERVISOR'],
      'reporting.read':['SUPERVISOR','TECHNICIAN','MANAGER_IT'],'reporting.export':['SUPERVISOR','TECHNICIAN','MANAGER_IT']
    };
    return access[permission]?.includes(role)??false;
  }
  private normalize(user:User):User{const role=user.role||user.roles?.[0]||'';return {...user,role,roles:role?[role]:[]}}
  private readUser():User|null{try{const user=JSON.parse(localStorage.getItem('stb_user')||'null');return user?this.normalize(user):null;}catch{return null;}}
}
