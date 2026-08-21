/** Administration des comptes, rôles fixes et activation/désactivation. */
import {Component,OnInit} from '@angular/core';
import {FormsModule} from '@angular/forms';
import {HttpClient} from '@angular/common/http';
import {AuthService,User} from '../core/auth.service';
import {CommonModule} from '@angular/common';

@Component({standalone:true,imports:[FormsModule,CommonModule],template:`
<section class="page">
  <div class="page-title"><div><h1>Gestion des utilisateurs</h1><p>Créer, rechercher, modifier et activer les comptes.</p></div><button class="primary" (click)="showCreate=!showCreate" *ngIf="auth.hasPermission('users.manage')">{{showCreate?'Fermer':'Nouvel utilisateur'}}</button></div>
  @if(message){<div class="success">{{message}}</div>} @if(error){<div class="error">{{error}}</div>}
  @if(showCreate){<form #createForm="ngForm" class="card form form-grid" (ngSubmit)="create()"><h2>Créer un utilisateur</h2><label>Username<input #username="ngModel" name="username" [(ngModel)]="draft.username" required minlength="3">@if(username.touched&&username.invalid){<small class="field-error">3 caractères minimum.</small>}</label><label>Prénom<input #firstName="ngModel" name="firstName" [(ngModel)]="draft.firstName" required>@if(firstName.touched&&firstName.invalid){<small class="field-error">Prénom obligatoire.</small>}</label><label>Nom<input #lastName="ngModel" name="lastName" [(ngModel)]="draft.lastName" required>@if(lastName.touched&&lastName.invalid){<small class="field-error">Nom obligatoire.</small>}</label><label>E-mail<input #email="ngModel" name="email" type="email" [(ngModel)]="draft.email" required email>@if(email.touched&&email.invalid){<small class="field-error">Adresse e-mail valide obligatoire.</small>}</label><label>Mot de passe<input #password="ngModel" name="password" type="password" minlength="8" [(ngModel)]="draft.password" required>@if(password.touched&&password.invalid){<small class="field-error">8 caractères minimum.</small>}</label><label>Rôle<select name="role" [(ngModel)]="draft.role" required>@for(role of roles;track role){<option [value]="role">{{role}}</option>}</select></label><button class="primary" [disabled]="createForm.invalid">Créer le compte</button></form>}
  <input class="search" placeholder="Rechercher par nom, username, e-mail, rôle ou statut" [(ngModel)]="query">
  <div class="card table-wrap"><table><thead><tr><th>Utilisateur</th><th>E-mail</th><th>Rôle</th><th>État</th><th>Actions</th></tr></thead><tbody>
  @for(user of filtered;track user.id){<tr><td><strong>{{user.firstName}} {{user.lastName}}</strong><br><small>{{user.username}}</small></td><td>{{user.email}}</td><td><span class="badge">{{user.role}}</span></td><td><span [class]="user.isActive?'status active':'status inactive'">{{user.isActive?'Actif':'Inactif'}}</span></td><td class="actions">@if(auth.hasPermission('users.manage')){<button (click)="edit(user)">Modifier</button><button (click)="active(user)">{{user.isActive?'Désactiver':'Activer'}}</button>}</td></tr>}
  </tbody></table></div>
  @if(selected){<div class="modal-backdrop" (click)="selected=null"><form #editForm="ngForm" class="card modal form" (click)="$event.stopPropagation()" (ngSubmit)="save()"><div class="page-title"><h2>Modifier {{selected.username}}</h2><button type="button" (click)="selected=null">×</button></div><label>Prénom<input name="editFirstName" [(ngModel)]="editDraft.firstName" required></label><label>Nom<input name="editLastName" [(ngModel)]="editDraft.lastName" required></label><label>E-mail<input name="editEmail" type="email" [(ngModel)]="editDraft.email" required email></label><label>Rôle<select name="editRole" [(ngModel)]="editDraft.role" required>@for(role of roles;track role){<option [value]="role">{{role}}</option>}</select></label>@if(editForm.touched&&editForm.invalid){<small class="field-error">Tous les champs doivent être valides.</small>}<button class="primary" [disabled]="editForm.invalid">Enregistrer</button></form></div>}
</section>`})
export class UsersComponent implements OnInit{
 users:User[]=[];readonly roles=['ADMIN','SUPERVISOR','TECHNICIAN','MANAGER_IT'];query='';showCreate=false;selected:User|null=null;message='';error='';readonly base='http://localhost:5041/api';
 draft={username:'',firstName:'',lastName:'',email:'',password:'',role:'TECHNICIAN'};editDraft={firstName:'',lastName:'',email:'',role:''};
 constructor(private http:HttpClient,public auth:AuthService){}
 get filtered(){const q=this.query.trim().toLowerCase();return this.users.filter(u=>!q||[u.firstName,u.lastName,u.username,u.email,u.role,u.isActive?'actif':'inactif'].some(v=>v.toLowerCase().includes(q)))}
 ngOnInit(){this.load()}
 load(){this.http.get<User[]>(`${this.base}/users`).subscribe({next:x=>this.users=x,error:e=>this.fail(e)})}
 create(){this.clear();this.http.post(`${this.base}/users`,this.draft).subscribe({next:()=>{this.draft={username:'',firstName:'',lastName:'',email:'',password:'',role:'TECHNICIAN'};this.showCreate=false;this.message='Utilisateur créé.';this.load()},error:e=>this.fail(e)})}
 edit(u:User){this.selected=u;this.editDraft={firstName:u.firstName,lastName:u.lastName,email:u.email,role:u.role}}
 save(){if(!this.selected)return;this.clear();this.http.put<User>(`${this.base}/users/${this.selected.id}`,this.editDraft).subscribe({next:()=>{this.selected=null;this.message='Utilisateur modifié.';this.load()},error:e=>this.fail(e)})}
 active(u:User){this.clear();this.http.patch(`${this.base}/users/${u.id}/active?value=${!u.isActive}`,{}).subscribe({next:()=>{u.isActive=!u.isActive;this.message=`Compte ${u.isActive?'activé':'désactivé'}.`},error:e=>this.fail(e)})}
 clear(){this.message='';this.error=''} fail(e:any){this.error=e.error?.message||'Opération impossible.'}
}
