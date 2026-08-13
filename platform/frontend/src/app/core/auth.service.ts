import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
export interface User { id:string; username:string; email:string; firstName:string; lastName:string; isActive:boolean; roles:string[]; createdAt:string; lastLoginAt?:string; }
interface LoginResponse { token:string; expiresAt:string; user:User; }
@Injectable({providedIn:'root'})
export class AuthService {
  private readonly api='http://localhost:5041/api';
  readonly currentUser=signal<User|null>(this.readUser());
  constructor(private http:HttpClient){}
  login(usernameOrEmail:string,password:string):Observable<LoginResponse>{return this.http.post<LoginResponse>(`${this.api}/auth/login`,{usernameOrEmail,password}).pipe(tap(x=>{localStorage.setItem('stb_token',x.token);localStorage.setItem('stb_user',JSON.stringify(x.user));this.currentUser.set(x.user);}));}
  me(){return this.http.get<User>(`${this.api}/auth/me`).pipe(tap(x=>{this.currentUser.set(x);localStorage.setItem('stb_user',JSON.stringify(x));}));}
  logout(){localStorage.removeItem('stb_token');localStorage.removeItem('stb_user');this.currentUser.set(null);}
  token(){return localStorage.getItem('stb_token');}
  private readUser():User|null{try{return JSON.parse(localStorage.getItem('stb_user')||'null');}catch{return null;}}
}
