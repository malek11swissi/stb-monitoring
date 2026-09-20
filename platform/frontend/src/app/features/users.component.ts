  /** Administration des comptes, rôles fixes et activation/désactivation. */
  import {Component,OnInit} from '@angular/core';
  import {FormsModule} from '@angular/forms';
  import {HttpClient} from '@angular/common/http';
  import {AuthService,User} from '../core/auth.service';
  import {CommonModule} from '@angular/common';
  import {API_ORIGIN,API_ROOT} from '../core/api-url';

  @Component({standalone:true,imports:[FormsModule,CommonModule],styleUrls:['./users.actions.css','./users.page.css'],template:`
  <section class="page users-page">
    <header class="users-hero">
      <div class="users-hero-copy"><span class="users-eyebrow"><i></i> ADMINISTRATION DES ACCÈS</span><h1>Utilisateurs</h1><p>Gérez les comptes et les rôles de votre équipe depuis un seul espace.</p></div>
      <button class="primary invite-trigger" type="button" (click)="showCreate=!showCreate" *ngIf="auth.hasPermission('users.manage')"><span>{{showCreate?'×':'+'}}</span>{{showCreate?'Fermer le formulaire':'Inviter un utilisateur'}}</button>
    </header>
    <div class="users-metrics"><article><span class="metric-symbol total-symbol">◉</span><div><small>COMPTES</small><b>{{users.length}}</b><span>utilisateurs enregistrés</span></div></article><article><span class="metric-symbol active-symbol">✓</span><div><small>ACTIFS</small><b>{{activeCount}}</b><span>accès autorisé</span></div></article><article><span class="metric-symbol inactive-symbol">Ⅱ</span><div><small>INACTIFS</small><b>{{inactiveCount}}</b><span>accès suspendu</span></div></article></div>
                @if(message)
                  {<div class="success">{{message}}</div>}
                  @if(error){<div class="error">{{error}}</div>}
                  @if(showCreate){<form #createForm="ngForm" class="card form form-grid invite-form" (ngSubmit)="create()">
                  <div class="form-intro"><span class="form-icon">✉</span><div><span class="users-eyebrow">NOUVEL ACCÈS</span><h2>Inviter un utilisateur</h2><p>Renseignez son identité et son rôle. Il définira son mot de passe via un lien sécurisé reçu par e-mail.</p></div></div>
        <label>Username<input #username="ngModel" name="username" [(ngModel)]="draft.username" required minlength="3">
          @if(username.touched&&username.invalid)
            {<small class="field-error">3 caractères minimum.</small>}
        </label>

        <label>Prénom<input #firstName="ngModel" name="firstName" [(ngModel)]="draft.firstName" required>
             @if(firstName.touched&&firstName.invalid)
               {<small class="field-error">Prénom obligatoire.</small>}
        </label>

         <label>Nom<input #lastName="ngModel" name="lastName" [(ngModel)]="draft.lastName" required>
             @if(lastName.touched&&lastName.invalid)
                {<small class="field-error">Nom obligatoire.</small>}
        </label>

        <label>E-mail<input #email="ngModel" name="email" type="email" [(ngModel)]="draft.email" required email>
           @if(email.touched&&email.invalid)
              {<small class="field-error">Adresse e-mail valide obligatoire.</small>}
        </label>
        
        <label>Rôle<select name="role" [(ngModel)]="draft.role" required>
          @for(role of roles;track role)
            {<option [value]="role">{{role}}</option>}
        </select>
      </label>
        
        <div class="form-footer"><span>🔒 Aucun mot de passe transmis par e-mail</span><button class="primary" [disabled]="createForm.invalid">Envoyer l’invitation →</button></div>
      </form>}

        <section class="directory-head"><div><span class="users-eyebrow">ANNUAIRE</span><h2>Comptes de l’équipe</h2><p>{{filtered.length}} résultat{{filtered.length>1?'s':''}} affiché{{filtered.length>1?'s':''}}</p></div><div class="directory-filters"><label class="search-field"><span>⌕</span><input placeholder="Nom, identifiant ou e-mail…" [(ngModel)]="query" aria-label="Rechercher un utilisateur"></label><label class="filter-select"><span>Rôle</span><select [(ngModel)]="roleFilter" aria-label="Filtrer par rôle"><option value="">Tous les rôles</option>@for(role of roles;track role){<option [value]="role">{{roleLabel(role)}}</option>}</select></label><label class="filter-select"><span>État</span><select [(ngModel)]="statusFilter" aria-label="Filtrer par état"><option value="">Tous</option><option value="active">Actifs</option><option value="inactive">Inactifs</option></select></label></div></section>
           <div class="card table-wrap users-table-wrap"><table class="users-table"><thead><tr><th>Utilisateur</th><th>E-mail</th><th>Rôle</th><th>État</th><th>Actions</th></tr></thead>
           <tbody>
                      @for(user of filtered;track user.id){<tr>
                        <td>
                        <div class="user-cell">
                        <img [src]="avatar(user)" alt="">
                                <span><strong>{{user.firstName}} {{user.lastName}}</strong>
                                      <small>&#64;{{user.username}} · {{user.badge||'Membre'}}</small>
                                </span>
                        </div>
                        </td>
                        <td>{{user.email}}</td>

                           <td>
                          <span class="role-badge" [attr.data-role]="user.role">{{roleLabel(user.role)}}</span>
                           </td>
                          <td><span [class]="user.isActive?'status active':'status inactive'">{{user.isActive?'Actif':'Inactif'}}

                          </span></td>
                          <td class="actions">@if(auth.hasPermission('users.manage'))
                            {<button class="user-action edit-action" type="button" (click)="edit(user)" [attr.aria-label]="'Modifier le compte de '+user.firstName+' '+user.lastName"><svg viewBox="0 0 24 24" aria-hidden="true"><path d="M12 20h9M16.5 3.5a2.12 2.12 0 0 1 3 3L9 17l-4 1 1-4L16.5 3.5Z"/></svg><span>Modifier</span></button>
                              <button class="user-action" [class.disable-action]="user.isActive" [class.enable-action]="!user.isActive" type="button" (click)="active(user)" [attr.aria-label]="(user.isActive?'Désactiver':'Activer')+' le compte de '+user.firstName+' '+user.lastName">@if(user.isActive){<svg viewBox="0 0 24 24" aria-hidden="true"><rect x="4" y="3" width="16" height="18" rx="4"/><path d="M10 9v6m4-6v6"/></svg>}@else{<svg viewBox="0 0 24 24" aria-hidden="true"><circle cx="12" cy="12" r="9"/><path d="m10 8 6 4-6 4V8Z"/></svg>}<span>{{user.isActive?'Désactiver':'Activer'}}</span></button>
                                  @if(eligibility[user.id]?.canDelete){<button class="danger-action danger-compact" (click)="deleteTarget=user" title="Supprimer ce compte sans activité"><span>⌫</span> Supprimer</button>}}
                          </td></tr>}
                      @empty{<tr><td colspan="5" class="users-empty"><span>⌕</span><b>Aucun utilisateur trouvé</b><small>Essayez une autre recherche ou réinitialisez les filtres.</small><button type="button" (click)="resetFilters()">Effacer les filtres</button></td></tr>}
              </tbody>
           </table>
        </div>

    @if(selected)
      {<div class="modal-backdrop" (click)="selected=null">
      <form #editForm="ngForm" class="card modal form user-edit-modal" (click)="$event.stopPropagation()" (ngSubmit)="save()">
        <div class="edit-modal-head"><div class="form-icon">✎</div><div><span class="users-eyebrow">ÉDITION DU COMPTE</span><h2>Modifier l’utilisateur</h2><p>&#64;{{selected.username}}</p></div><button type="button" class="modal-close" (click)="selected=null" aria-label="Fermer">×</button></div>
        
        <label>Prénom<input name="editFirstName" [(ngModel)]="editDraft.firstName" required></label>
        <label>Nom<input name="editLastName" [(ngModel)]="editDraft.lastName" required></label>
        <label>E-mail<input name="editEmail" type="email" [(ngModel)]="editDraft.email" required email></label>
        <label>Rôle
          <select name="editRole" [(ngModel)]="editDraft.role" required>@for(role of roles;track role)
          {<option [value]="role">{{role}}</option>}
            </select>
        </label>
           @if(editForm.touched&&editForm.invalid)
            {<small class="field-error">Tous les champs doivent être valides.</small>}
              <div class="edit-modal-actions"><button type="button" (click)="selected=null">Annuler</button><button class="primary" [disabled]="editForm.invalid">Enregistrer les modifications</button></div>
        </form>
      </div>}


    @if(deleteTarget){<div class="modal-backdrop" (click)="deleteTarget=null">
      <section class="card modal delete-modal" (click)="$event.stopPropagation()">
        <div class="danger-icon">⌫</div><span class="danger-kicker">ZONE SENSIBLE</span>
          <h2>Supprimer ce compte ?</h2>
          <div class="delete-user-preview"><img [src]="avatar(deleteTarget)" alt=""><span><strong>{{deleteTarget.firstName}} {{deleteTarget.lastName}}</strong><small>&#64;{{deleteTarget.username}} · {{deleteTarget.role}}</small></span></div>
          <p>Cette suppression est définitive. Le compte ne pourra plus accéder à STB Monitoring.</p>
        <div class="warning-box"><b>Suppression autorisée</b><span>Ce compte ne possède aucune activité métier liée. Cette opération ne peut pas être annulée.</span></div>
        <div class="modal-actions">
          <button [disabled]="deleting" (click)="deleteTarget=null">Conserver le compte</button>
          <button class="danger-action danger-confirm" [disabled]="deleting" (click)="removePermanently()">{{deleting?'Suppression…':'⌫ Supprimer définitivement'}}</button>
        </div></section></div>}
  </section>`,styles:[`.user-cell{display:flex;align-items:center;gap:11px}.user-cell img{width:42px;height:42px;object-fit:cover;border-radius:12px;background:#eaf4f6;border:1px solid #d7e5e9}.user-cell span{display:grid}.user-cell small{margin-top:3px;color:#61727e}.danger-action{color:#fff!important;background:#b42318!important;border-color:#b42318!important}.danger-action:hover{background:#8f1c13!important;transform:translateY(-1px)}`]})
  
  export class UsersComponent implements OnInit{

  users:User[]=[];
  readonly roles=['ADMIN','SUPERVISOR','TECHNICIAN','MANAGER_IT'];
  query='';
  roleFilter='';
  statusFilter='';
  showCreate=false;
  selected:User|null=null;
  deleteTarget:User|null=null;
  deleting=false;
  eligibility:Partial<Record<string,{canDelete:boolean;reason:string}>>={};
  message='';
  error='';
  readonly base=API_ROOT;
  draft={username:'',firstName:'',lastName:'',email:'',role:'TECHNICIAN'};
  editDraft={firstName:'',lastName:'',email:'',role:''};
  
  constructor(private http:HttpClient,public auth:AuthService){}

  get filtered()
     {const q=this.query.trim().toLowerCase();
        return this.users.filter
          (u=>(!this.roleFilter||u.role===this.roleFilter)&&(!this.statusFilter||(this.statusFilter==='active')===u.isActive)&&(!q||[u.firstName,u.lastName,u.username,u.email,u.role,u.isActive?'actif':'inactif']
          .some(v=>v.toLowerCase().includes(q))))}
  get activeCount(){return this.users.filter(u=>u.isActive).length}
  get inactiveCount(){return this.users.length-this.activeCount}
  roleLabel(role:string){return ({ADMIN:'Administrateur',SUPERVISOR:'Superviseur',TECHNICIAN:'Technicien',MANAGER_IT:'Manager IT'} as Record<string,string>)[role]||role}
  resetFilters(){this.query='';this.roleFilter='';this.statusFilter=''}
 
  ngOnInit()
  {this.load()}

  load()
    {this.http.get<User[]>(`${this.base}/users`)
    .subscribe({next:x=>{this.users=x;this.loadEligibility()},error:e=>this.fail(e)})}

  loadEligibility(){
    this.eligibility={};
     if(!this.auth.hasPermission('users.manage'))return;
     for(const user of this.users)
      this.http.get<{canDelete:boolean;reason:string}>
     (`${this.base}/users/${user.id}/deletion-eligibility`).subscribe ({next:x=>this.eligibility[user.id]=x})}

  create(){this.clear();
    this.http.post(`${this.base}/users`,this.draft)
    .subscribe({next:()=>{this.draft={username:'',firstName:'',lastName:'',email:'',role:'TECHNICIAN'};
    this.showCreate=false;
    this.message='Utilisateur créé et invitation envoyée.';
    this.load()},error:e=>this.fail(e)})}
  
  edit(u:User){this.selected=u;
    this.editDraft={firstName:u.firstName,lastName:u.lastName,email:u.email,role:u.role}}

  save(){if(!this.selected)return;
    this.clear();
    this.http.put<User>(`${this.base}/users/${this.selected.id}`,this.editDraft)
    .subscribe ({next:()=>{this.selected=null;this.message='Utilisateur modifié.';
      this.load()},error:e=>this.fail(e)})}

  active(u:User){this.clear();this.http.patch(`${this.base}/users/${u.id}/active?value=${!u.isActive}`,{})
  .subscribe({next:()=>{u.isActive=!u.isActive;
    this.message=`Compte ${u.isActive?'activé':'désactivé'}.`}, error:e=>this.fail(e)})}
  removePermanently(){if(!this.deleteTarget)return;
    
    const username=this.deleteTarget.username;this.clear();this.deleting=true;
    this.http.delete(`${this.base}/users/${this.deleteTarget.id}`)
    .subscribe({next:()=>{this.deleteTarget=null;
     this.deleting=false;
     this.message=`Le compte ${username} a été supprimé définitivement.`;
    this.load()},error:e=>{this.deleting=false;this.deleteTarget=null;this.fail(e)}})}

  avatar(u:User){return u.avatarUrl?`${API_ORIGIN}${u.avatarUrl}`:'/default-avatar.svg'}
  clear(){this.message='';this.error=''} fail(e:any){this.error=e.error?.message||'Opération impossible.'}
  }
