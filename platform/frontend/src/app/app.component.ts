/** Coquille principale : navigation adaptée au rôle, session et zone de contenu. */
import { Component } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { NgIf } from '@angular/common';
import { AuthService } from './core/auth.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, NgIf],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent {
  title='StbFrontend';
  sidebarOpen=false;
  constructor(public auth:AuthService,private router:Router){}
  logout(){this.auth.logout();this.router.navigate(['/login']);}
  closeSidebar(){this.sidebarOpen=false;}
}
