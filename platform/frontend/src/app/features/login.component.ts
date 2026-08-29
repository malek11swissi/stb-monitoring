/** Page publique de connexion et retour vers la page initialement demandée. */
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../core/auth.service';

@Component({
  standalone: true,
  imports: [FormsModule,RouterLink],
  styleUrls: ['./login.animation.css','./login.radar.css','./login.logo.css','./login.radar-large.css','./login.copy.css','./login.radar-position.css','./login.radar-hud.css'],
  template: `
    <main class="auth-page">
      <section class="brand-panel" aria-label="Présentation de STB Sentinel">
        <div class="brand">
          <span class="brand-mark stb-logo"><img src="/assets/stb-logo.jpg" alt="Logo STB"></span>
          <div><strong>STB Monitoring</strong><small>Centre de supervision intelligent</small></div>
        </div>

        <div class="brand-content">
          <span class="eyebrow"><i></i> Disponibilité · sécurité · anticipation</span>
          <h1>Gardez vos systèmes<br><em>sous contrôle.</em></h1>
          <p>Détectez les anomalies plus tôt et accélérez la résolution grâce à une supervision enrichie par l’IA.</p>
          <section class="monitor-radar" aria-label="Illustration animée de la supervision intelligente">
            <div class="radar-stage"><span class="hud hud-availability"><i>✓</i><small>Disponibilité</small><b>99,98 %</b></span><span class="hud hud-systems"><i>◉</i><small>SI surveillés</small><b>4 actifs</b></span><div class="radar-disc"><i class="ring ring-one"></i><i class="ring ring-two"></i><i class="cross horizontal"></i><i class="cross vertical"></i><i class="sweep"></i><span class="radar-core"><b>S</b><small>Monitoring</small></span><span class="system-node core"><i></i><b>CORE</b><small>UP</small></span><span class="system-node rne"><i></i><b>RNE</b><small>UP</small></span><span class="system-node sms"><i></i><b>SMS</b><small>UP</small></span><span class="system-node rh"><i></i><b>RH</b><small>DEGRADED</small></span><span class="signal signal-one"></span><span class="signal signal-two"></span></div><span class="hud hud-latency"><i>⌁</i><small>Latence moyenne</small><b>142 ms</b></span><span class="hud hud-tls"><i>⌾</i><small>Sécurité TLS</small><b>Validée</b></span></div>
            <footer class="radar-status"><span><i class="online"></i> 3 systèmes disponibles</span><span><i class="warning"></i> 1 anomalie anticipée</span><span><i class="ai"></i> Modèle IA actif</span></footer>
          </section>
        </div>

        <footer><span><i></i> Services de supervision opérationnels</span><small>STB · Direction des Systèmes d'Information</small></footer>
      </section>

      <section class="form-panel">
        <div class="mobile-brand"><span class="brand-mark stb-logo"><img src="/assets/stb-logo.jpg" alt="Logo STB"></span><strong>STB Monitoring</strong></div>
        <div class="login-card">
          <header>
            <span class="welcome-icon">↗</span>
            <h2>Bienvenue</h2>
            <p>Connectez-vous à votre espace de supervision.</p>
          </header>

          <form #loginForm="ngForm" (ngSubmit)="submit()" novalidate>
            @if(twoFactorRequired){<div class="security-note"><span>⌾</span><p><b>Vérification administrateur</b><small>Un code à 6 chiffres a été envoyé à votre adresse e-mail. Il expire dans 5 minutes.</small></p></div><label for="twoFactorCode">Code de sécurité</label><div class="control"><span>✦</span><input id="twoFactorCode" name="twoFactorCode" [(ngModel)]="twoFactorCode" required pattern="[0-9]{6}" maxlength="6" inputmode="numeric" autocomplete="one-time-code" placeholder="000000" [disabled]="loading"></div>}
            @if(!twoFactorRequired){
            <label for="login">Nom d'utilisateur ou adresse e-mail</label>
            <div class="control" [class.invalid]="loginField.touched && loginField.invalid">
              <span aria-hidden="true">○</span>
              <input id="login" #loginField="ngModel" name="login" [(ngModel)]="login" required autocomplete="username" placeholder="exemple@stb.com.tn" [disabled]="loading">
            </div>
            @if(loginField.touched && loginField.invalid){<small class="field-error">Saisissez votre identifiant.</small>}

            <div class="label-row"><label for="password">Mot de passe</label><small>8 caractères minimum</small></div>
            <div class="control" [class.invalid]="passwordField.touched && passwordField.invalid">
              <span aria-hidden="true">⌑</span>
              <input id="password" #passwordField="ngModel" name="password" [(ngModel)]="password" [type]="showPassword?'text':'password'" required minlength="8" autocomplete="current-password" placeholder="Votre mot de passe" [disabled]="loading">
              <button type="button" class="reveal" (click)="showPassword=!showPassword" [attr.aria-label]="showPassword?'Masquer le mot de passe':'Afficher le mot de passe'">{{showPassword?'Masquer':'Afficher'}}</button>
            </div>
            @if(passwordField.touched && passwordField.invalid){<small class="field-error">Le mot de passe doit contenir au moins 8 caractères.</small>}
            }

            @if(error){<div class="alert" role="alert"><span>!</span><p>{{error}}</p></div>}

            <button class="submit" [disabled]="loading || loginForm.invalid">
              @if(loading){<i class="spinner"></i><span>Vérification…</span>}@else{<span>{{twoFactorRequired?'Valider le code':'Se connecter'}}</span><b>→</b>}
            </button>
            <a routerLink="/forgot-password" style="justify-self:end;color:#087e76;font-size:12px;font-weight:750;text-decoration:none;margin-top:4px">Mot de passe oublié ?</a>
          </form>

          <div class="security-note"><span>⌾</span><p><b>Connexion sécurisée</b><small>Vos accès sont chiffrés et les actions sensibles sont journalisées.</small></p></div>
        </div>
        <p class="support">Besoin d'aide ? Contactez l'administrateur de la plateforme.</p>
      </section>
    </main>
  `,
  styles: [`
    :host{display:block;min-height:100vh;color:#172b3a}.auth-page{min-height:100vh;display:grid;grid-template-columns:minmax(480px,1.08fr) minmax(460px,.92fr);background:#f4f7f8}.brand-panel{position:relative;overflow:hidden;display:flex;flex-direction:column;padding:42px 56px;color:#fff;background:radial-gradient(circle at 15% 12%,#157d7a55 0 22%,transparent 47%),radial-gradient(circle at 90% 90%,#123d6045,transparent 45%),linear-gradient(145deg,#071b2a,#0b2939 58%,#0b3441)}.brand-panel:before,.brand-panel:after{content:"";position:absolute;border:1px solid #ffffff0d;border-radius:50%}.brand-panel:before{width:520px;height:520px;right:-260px;top:15%}.brand-panel:after{width:360px;height:360px;left:-230px;bottom:-160px}.brand{display:flex;align-items:center;gap:13px;position:relative;z-index:1}.brand-mark{width:46px;height:46px;border-radius:14px;display:grid;place-items:center;background:linear-gradient(145deg,#1cc0ad,#087d76);color:#fff;font-size:23px;font-weight:900;box-shadow:0 12px 30px #00b6a933}.brand div{display:grid}.brand strong{font-size:18px}.brand small{color:#91aab6;text-transform:uppercase;letter-spacing:.17em;font-size:9px}.brand-content{margin:auto 0;max-width:640px;position:relative;z-index:1}.eyebrow{display:inline-flex;align-items:center;gap:9px;padding:7px 11px;border:1px solid #37d5c73a;border-radius:99px;background:#0fb4a313;color:#7de3d8;font-size:11px;font-weight:800;letter-spacing:.08em;text-transform:uppercase}.eyebrow i,footer i{width:7px;height:7px;background:#34d399;border-radius:50%;box-shadow:0 0 0 5px #34d39917}.brand-content h1{font-size:clamp(38px,4vw,58px);line-height:1.08;letter-spacing:-.045em;margin:24px 0 18px}.brand-content h1 em{font-style:normal;color:#52d5c8}.brand-content>p{font-size:16px;line-height:1.75;color:#a8bec8;max-width:570px}.features{display:grid;gap:17px;margin-top:38px}.features article{display:flex;gap:14px;align-items:center}.features article>span{width:42px;height:42px;border:1px solid #5ee2d72e;background:#19a99b12;color:#65ddd2;border-radius:12px;display:grid;place-items:center;font-size:18px}.features article div{display:grid;gap:3px}.features b{font-size:14px}.features small{color:#8da8b4}.brand-panel footer{display:flex;justify-content:space-between;align-items:center;position:relative;z-index:1;color:#7998a5}.brand-panel footer span{display:flex;align-items:center;gap:9px;font-size:11px}.brand-panel footer small{font-size:10px}.form-panel{display:grid;place-items:center;padding:42px clamp(28px,6vw,90px);position:relative}.login-card{width:min(440px,100%)}.login-card header{margin-bottom:34px}.welcome-icon{width:44px;height:44px;display:grid;place-items:center;border-radius:13px;background:#e5f7f4;color:#087e76;font-size:20px;font-weight:900}.login-card h2{font-size:34px;letter-spacing:-.035em;margin:18px 0 6px;color:#122a3b}.login-card header p{margin:0;color:#6d808c}.login-card form{display:grid;gap:9px}.login-card label{font-size:12px;font-weight:800;color:#2b4250}.label-row{display:flex;justify-content:space-between;align-items:end;margin-top:11px}.label-row small{color:#82939d;font-size:10px}.control{height:52px;display:flex;align-items:center;gap:10px;border:1px solid #cbd8dd;border-radius:12px;padding:0 14px;background:#fff;transition:.2s;box-shadow:0 2px 5px #17364d05}.control:focus-within{border-color:#12958b;box-shadow:0 0 0 4px #12958b15}.control.invalid{border-color:#d64f4f}.control>span{color:#75909b}.control input{min-width:0;flex:1;border:0;outline:0;padding:0;background:transparent;color:#172b3a;font:inherit}.control input::placeholder{color:#a3b0b7}.reveal{border:0;background:transparent;color:#087e76;font-size:10px;font-weight:800;padding:5px}.field-error{color:#b42318;font-size:11px}.alert{display:flex;align-items:center;gap:10px;background:#fff0ef;border:1px solid #f4c7c3;color:#a62b22;border-radius:11px;padding:10px 12px;margin-top:8px}.alert>span{width:22px;height:22px;border-radius:50%;display:grid;place-items:center;background:#d7473b;color:#fff;font-weight:900}.alert p{margin:0;font-size:12px}.submit{height:52px;margin-top:14px;border:0;border-radius:12px;padding:0 18px;display:flex;align-items:center;justify-content:center;gap:10px;background:linear-gradient(135deg,#0c8f85,#08736f);color:#fff;font:inherit;font-weight:800;box-shadow:0 12px 24px #0b8a8030;transition:.2s}.submit b{margin-left:auto;font-size:19px}.submit span:first-child:not(:last-child){margin-left:auto}.submit:hover:not(:disabled){transform:translateY(-1px);box-shadow:0 15px 28px #0b8a8040}.submit:disabled{opacity:.58;cursor:not-allowed;box-shadow:none}.spinner{width:17px;height:17px;border:2px solid #ffffff55;border-top-color:#fff;border-radius:50%;animation:spin .7s linear infinite}.security-note{display:flex;gap:11px;padding:14px;margin-top:24px;border-radius:12px;background:#edf4f5;color:#56707b}.security-note>span{color:#0c8f85}.security-note p{display:grid;margin:0;gap:2px}.security-note b{font-size:11px;color:#38515c}.security-note small{font-size:10px;line-height:1.4}.support{position:absolute;bottom:22px;margin:0;color:#81919a;font-size:10px}.mobile-brand{display:none}@keyframes spin{to{transform:rotate(360deg)}}@media(max-width:900px){.auth-page{grid-template-columns:1fr}.brand-panel{display:none}.form-panel{min-height:100vh;padding:95px 24px 70px}.mobile-brand{display:flex;position:absolute;top:27px;left:28px;align-items:center;gap:10px}.mobile-brand .brand-mark{width:36px;height:36px;border-radius:10px;font-size:18px}.login-card h2{font-size:30px}.support{left:24px;right:24px;text-align:center}}@media(max-width:420px){.form-panel{padding-left:18px;padding-right:18px}.login-card header{margin-bottom:26px}.control{height:50px}}
  `]
})
export class LoginComponent {
  login=''; password='';twoFactorCode='';challengeToken='';twoFactorRequired=false; error=''; loading=false; showPassword=false;
  constructor(private auth:AuthService,private router:Router,private route:ActivatedRoute){}
  submit(){
    if(this.loading)return;
    this.loading=true;this.error='';
    const request=this.twoFactorRequired?this.auth.verifyTwoFactor(this.challengeToken,this.twoFactorCode):this.auth.login(this.login.trim(),this.password);
    request.subscribe({
      next:result=>{if(result.requiresTwoFactor&&result.challengeToken){this.challengeToken=result.challengeToken;this.twoFactorRequired=true;this.password='';this.loading=false;return}this.router.navigateByUrl(this.safeReturnUrl())},
      error:e=>{this.error=e.status===0?'Le service est momentanément indisponible. Réessayez dans quelques instants.':e.error?.message||'Identifiants incorrects ou compte désactivé.';this.loading=false}
    });
  }
  private safeReturnUrl(){const value=this.route.snapshot.queryParamMap.get('returnUrl');return value?.startsWith('/')&&!value.startsWith('//')?value:'/dashboard'}
}
