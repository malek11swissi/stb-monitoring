/** Coquille principale : navigation adaptée au rôle, session et zone de contenu. */
import { Component, OnInit } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { NgIf } from '@angular/common';
import { AuthService } from './core/auth.service';
import { OperationsService } from './core/operations.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, NgIf],
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css','./app.theme.css','./app.modern.css']
})
export class AppComponent implements OnInit {
  title='StbFrontend';
  sidebarOpen=false;
  unreadCount=0;
  darkMode=localStorage.getItem('stb_theme')==='dark';
  constructor(public auth:AuthService,private router:Router,private operations:OperationsService){}
  ngOnInit(){if(this.auth.token())this.operations.notifications().subscribe({next:x=>this.unreadCount=x.filter(n=>!n.isRead).length,error:()=>{}})}
  logout(){this.auth.logout();this.router.navigate(['/login']);}
  closeSidebar(){this.sidebarOpen=false;}
  avatar(user:{avatarUrl?:string}){return user.avatarUrl?`http://localhost:5041${user.avatarUrl}`:'/default-avatar.svg';}
  roleLabel(role:string){return({ADMIN:'Administrateur',SUPERVISOR:'Superviseur',TECHNICIAN:'Technicien',MANAGER_IT:'Manager IT'}as Record<string,string>)[role]||role;}
  toggleTheme(){this.darkMode=!this.darkMode;localStorage.setItem('stb_theme',this.darkMode?'dark':'light')}
}
