/** Flux personnel avec filtres essentiels adaptés au rôle connecté. */
import {Component,OnInit} from '@angular/core';
import {CommonModule} from '@angular/common';
import {Router} from '@angular/router';
import {OperationsService,NotificationItem} from '../core/operations.service';
import {AuthService} from '../core/auth.service';

type NotificationFilter='all'|'unread'|'account'|'incident'|'alert'|'sla';
@Component({standalone:true,imports:[CommonModule],template:`
<section class="page notifications"><header class="head"><div><span>FLUX PERSONNEL</span><h1>Notifications</h1><p>Filtrez rapidement les événements importants pour votre rôle.</p></div><button (click)="readAll()">✓ Tout marquer comme lu</button></header>
  <nav class="filters" aria-label="Filtres des notifications">
    <button [class.active]="filter==='all'" (click)="filter='all'"><i>▦</i><span>Toutes</span><b>{{items.length}}</b></button>
    <button [class.active]="filter==='unread'" (click)="filter='unread'"><i>●</i><span>Non lues</span><b>{{unread}}</b></button>
    @if(role==='ADMIN'){<button [class.active]="filter==='account'" (click)="filter='account'"><i>♙</i><span>Comptes</span></button>}
    @if(role!=='ADMIN'){<button [class.active]="filter==='incident'" (click)="filter='incident'"><i>◆</i><span>Incidents</span></button>}
    @if(role==='SUPERVISOR'){<button [class.active]="filter==='alert'" (click)="filter='alert'"><i>!</i><span>Alertes</span></button>}
    @if(role==='SUPERVISOR'||role==='MANAGER_IT'){<button [class.active]="filter==='sla'" (click)="filter='sla'"><i>◷</i><span>SLA</span></button>}
  </nav>
  <div class="feed">@for(n of filtered;track n.id){<article [class]="n.isRead?'notice read':'notice'" (click)="open(n)"><div [class]="'notice-icon '+n.severity.toLowerCase()">{{icon(n.type)}}</div><div><div class="line"><strong>{{n.title}}</strong><span>{{n.createdAt|date:'dd/MM/yyyy HH:mm'}}</span></div><p>{{n.message}}</p><small>{{label(n.type)}}</small></div>@if(!n.isRead){<i class="unread-dot"></i>}</article>}@empty{<div class="empty"><b>Vous êtes à jour</b><span>Aucune notification dans cette catégorie.</span></div>}</div>
</section>`,styles:[`
.notifications{max-width:960px}.head{display:flex;justify-content:space-between;align-items:end}.head>div>span{font-size:10px;font-weight:900;letter-spacing:.16em;color:#0b8a80}.head h1{font-size:34px;margin:6px 0}.head p{color:#718294;margin:0}.head button{border:1px solid #c8dcdf;background:#f2fbfa;color:#087e76;padding:10px 14px;border-radius:10px;font-weight:800}.filters{display:flex;gap:8px;margin:25px 0 14px;flex-wrap:wrap}.filters button{display:flex;align-items:center;gap:7px;border:1px solid #dbe5e9;background:#fff;padding:10px 13px;border-radius:11px;color:#607582}.filters button.active{background:#123b5d;border-color:#123b5d;color:#fff;box-shadow:0 7px 18px #123b5d25}.filters i{font-style:normal;width:21px;height:21px;display:grid;place-items:center;border-radius:6px;background:#edf4f5}.filters .active i{background:#ffffff20}.filters b{margin-left:2px}.feed{display:grid;gap:10px}.notice{display:grid;grid-template-columns:auto 1fr auto;gap:14px;align-items:center;background:#fff;border:1px solid #dfe8ed;border-radius:14px;padding:17px;cursor:pointer;box-shadow:0 5px 16px #17364e08;transition:.16s}.notice:hover{transform:translateY(-1px);border-color:#a9ced0}.notice.read{opacity:.65}.notice-icon{width:44px;height:44px;border-radius:12px;display:grid;place-items:center;background:#e9f2ff;color:#286bb5;font-size:18px;font-weight:900}.notice-icon.critical{background:#feeceb;color:#b42318}.notice-icon.major{background:#fff0e6;color:#b94b0a}.line{display:flex;justify-content:space-between;gap:15px}.line span,.notice small{color:#8192a0;font-size:11px}.notice p{color:#536a7a;margin:5px 0}.unread-dot{width:9px;height:9px;border-radius:50%;background:#0ea5a0}.empty{display:grid;text-align:center;padding:60px;color:#718294}.empty b{color:#2a4051;font-size:18px}@media(max-width:600px){.head{align-items:start;flex-direction:column;gap:15px}.line{flex-direction:column}.filters button span{display:none}}
`]})
export class NotificationsComponent implements OnInit{
 items:NotificationItem[]=[];filter:NotificationFilter='all';constructor(private api:OperationsService,private router:Router,private auth:AuthService){}
 ngOnInit(){this.load()}get role(){return this.auth.currentUser()?.role}load(){this.api.notifications().subscribe(x=>this.items=x)}get unread(){return this.items.filter(x=>!x.isRead).length}
 get filtered(){return this.items.filter(n=>this.filter==='all'||(this.filter==='unread'&&!n.isRead)||(this.filter==='account'&&n.type.includes('USER_'))||(this.filter==='incident'&&n.type.includes('INCIDENT'))||(this.filter==='alert'&&n.type.includes('ALERT'))||(this.filter==='sla'&&(n.type.includes('SLA')||n.type.includes('ESCALAT'))))}
 icon(t:string){return t.includes('USER_')?'♙':t.includes('ALERT')?'!':t.includes('SLA')||t.includes('ESCALAT')?'◷':t.includes('INCIDENT')?'◆':'●'}label(t:string){return t.includes('USER_')?'Gestion des comptes':t.replaceAll('_',' ')}
 open(n:NotificationItem){const go=()=>n.actionUrl&&this.router.navigateByUrl(n.actionUrl);n.isRead?go():this.api.read(n.id).subscribe(()=>{n.isRead=true;go()})}readAll(){this.api.readAll().subscribe(()=>this.load())}
}
