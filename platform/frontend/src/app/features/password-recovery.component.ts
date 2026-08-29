import {Component} from '@angular/core';
import {FormsModule} from '@angular/forms';
import {HttpClient} from '@angular/common/http';
import {ActivatedRoute,RouterLink} from '@angular/router';

@Component({standalone:true,imports:[FormsModule,RouterLink],template:`
<main class="recovery"><section class="card">
  <a routerLink="/login" class="back">← Retour à la connexion</a>
  @if(!token){
    <span class="icon">✉</span><h1>Mot de passe oublié</h1>
    <p>Indiquez votre adresse professionnelle. Si le compte existe, vous recevrez un lien valable 30 minutes.</p>
    <form #forgot="ngForm" (ngSubmit)="request()"><label>Adresse e-mail<input name="email" [(ngModel)]="email" type="email" required email autocomplete="email"></label><button class="primary" [disabled]="forgot.invalid||loading">Envoyer le lien</button></form>
  }@else{
    <span class="icon">⌾</span><h1>Nouveau mot de passe</h1><p>Choisissez un mot de passe fort et unique.</p>
    <form #reset="ngForm" (ngSubmit)="submit()">
      <label>Nouveau mot de passe<div class="password-control"><input name="password" [(ngModel)]="password" [type]="showPassword?'text':'password'" required minlength="10" autocomplete="new-password"><button type="button" (click)="showPassword=!showPassword" [attr.aria-label]="showPassword?'Masquer le mot de passe':'Afficher le mot de passe'">{{showPassword?'Masquer':'Afficher'}}</button></div></label>
      <div class="meter" [attr.aria-label]="'Force du mot de passe '+score+' sur 5'"><i [style.width.%]="score*20" [class.complete]="score===5"></i></div>
      <ul><li [class.ok]="password.length>=10">10 caractères minimum</li><li [class.ok]="upper">Une majuscule</li><li [class.ok]="lower">Une minuscule</li><li [class.ok]="digit">Un chiffre</li><li [class.ok]="special">Un caractère spécial</li></ul>
      <label>Confirmer le mot de passe<div class="password-control" [class.invalid]="confirmation.length>0&&!passwordsMatch"><input name="confirmation" [(ngModel)]="confirmation" [type]="showConfirmation?'text':'password'" required autocomplete="new-password"><button type="button" (click)="showConfirmation=!showConfirmation" [attr.aria-label]="showConfirmation?'Masquer la confirmation':'Afficher la confirmation'">{{showConfirmation?'Masquer':'Afficher'}}</button></div></label>
      @if(confirmation.length>0&&!passwordsMatch){<small class="field-error">Les deux mots de passe ne correspondent pas.</small>}
      @if(passwordsMatch&&confirmation){<small class="match">✓ Les mots de passe correspondent.</small>}
      <button class="primary" [disabled]="reset.invalid||score<5||!passwordsMatch||loading">{{loading?'Réinitialisation…':'Réinitialiser le mot de passe'}}</button>
    </form>
  }
  @if(message){<div class="success">{{message}}</div>} @if(error){<div class="error">{{error}}</div>}
</section></main>`,styles:[`
.recovery{min-height:100vh;display:grid;place-items:center;padding:24px;background:radial-gradient(circle at top left,#087e8b18,transparent 35%),#f5f8fa}.card{width:min(500px,100%);padding:34px}.icon{display:grid;place-items:center;width:48px;height:48px;border-radius:14px;background:#e3f5f4;color:#087e8b;font-size:22px}.back{display:inline-block;margin-bottom:28px;color:#087e8b;text-decoration:none}.card h1{margin:18px 0 8px;color:#123b5d}.card>p{color:#61727e;line-height:1.6}.card form{display:grid;gap:14px;margin-top:24px}.password-control{display:flex;align-items:center;border:1px solid #aabcc5;border-radius:9px;background:#fff;overflow:hidden;transition:.2s}.password-control:focus-within{border-color:#087e8b;box-shadow:0 0 0 3px #087e8b18}.password-control.invalid{border-color:#d64545}.password-control input{flex:1;min-width:0;border:0!important;box-shadow:none!important}.password-control button{border:0;background:transparent;color:#087e8b;font-weight:700;font-size:.76rem;padding:10px;cursor:pointer}.meter{height:7px;border-radius:8px;background:#e4eaed;overflow:hidden}.meter i{display:block;height:100%;background:#e89a19;transition:.2s}.meter i.complete{background:#198754}.card ul{list-style:none;padding:0;margin:0;display:grid;grid-template-columns:1fr 1fr;gap:8px;color:#61727e;font-size:.85rem}.card li:before{content:'○';margin-right:6px}.card li.ok{color:#198754}.card li.ok:before{content:'✓'}.field-error{color:#b42318}.match{color:#198754}.success,.error{margin-top:18px}@media(max-width:520px){.card ul{grid-template-columns:1fr}}
`]})
export class PasswordRecoveryComponent{
  readonly api='http://localhost:5041/api/auth';token='';email='';password='';confirmation='';message='';error='';loading=false;showPassword=false;showConfirmation=false;
  constructor(route:ActivatedRoute,private http:HttpClient){this.token=route.snapshot.queryParamMap.get('token')||''}
  get upper(){return /[A-Z]/.test(this.password)}get lower(){return /[a-z]/.test(this.password)}get digit(){return /\d/.test(this.password)}get special(){return /[^A-Za-z0-9]/.test(this.password)}get score(){return [this.password.length>=10,this.upper,this.lower,this.digit,this.special].filter(Boolean).length}get passwordsMatch(){return this.password.length>0&&this.password===this.confirmation}
  request(){this.loading=true;this.error='';this.http.post<any>(`${this.api}/forgot-password`,{email:this.email}).subscribe({next:x=>{this.message=x.message;this.loading=false},error:()=>{this.message='Si cette adresse correspond à un compte actif, un e-mail sera envoyé.';this.loading=false}})}
  submit(){if(!this.passwordsMatch||this.score<5)return;this.loading=true;this.error='';this.http.post(`${this.api}/reset-password`,{token:this.token,newPassword:this.password}).subscribe({next:()=>{this.password='';this.confirmation='';this.message='Mot de passe modifié. Vous pouvez maintenant vous connecter.';this.loading=false},error:e=>{this.error=e.error?.message||'Lien invalide ou expiré.';this.loading=false}})}
}
